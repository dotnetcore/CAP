using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using Google.Cloud.Spanner.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.GoogleSpanner
{
    internal class GoogleSpannerMonitoringApi : IMonitoringApi
    {
        private readonly GoogleSpannerOptions _options;
        private readonly string _pubName;
        private readonly string _recName;

        public GoogleSpannerMonitoringApi(IOptions<GoogleSpannerOptions> options, IStorageInitializer initializer)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
        }

        public async Task<MediumMessage> GetPublishedMessageAsync(long id) => await GetMessageAsync(_pubName, id);

        public async Task<MediumMessage> GetReceivedMessageAsync(long id) => await GetMessageAsync(_recName, id);

        public StatisticsDto GetStatistics()
        {
            var sql = $@"
    SELECT
    (
        SELECT COUNT(Id) FROM {_pubName} WHERE StatusName = 'Succeeded'
    ) AS PublishedSucceeded,
    (
        SELECT COUNT(Id) FROM {_recName} WHERE StatusName = 'Succeeded'
    ) AS ReceivedSucceeded,
    (
        SELECT COUNT(Id) FROM {_pubName} WHERE StatusName = 'Failed'
    ) AS PublishedFailed,
    (
        SELECT COUNT(Id) FROM {_recName} WHERE StatusName = 'Failed'
    ) AS ReceivedFailed";

            StatisticsDto statistics = new StatisticsDto();
            using (var connection = new SpannerConnection(_options.ConnectionString))
            {
                var cmd = connection.CreateSelectCommand(sql);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        statistics.PublishedSucceeded = reader.GetInt32(0);
                        statistics.ReceivedSucceeded = reader.GetInt32(1);
                        statistics.PublishedFailed = reader.GetInt32(2);
                        statistics.ReceivedFailed = reader.GetInt32(3);
                    }
                }
            }
            return statistics;
        }

        public PagedQueryResult<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? _pubName : _recName;
            var where = string.Empty;

            if (!string.IsNullOrEmpty(queryDto.StatusName)) where += " and Lower(StatusName) = Lower(@StatusName)";

            if (!string.IsNullOrEmpty(queryDto.Name)) where += " and Lower(Name) = Lower(@Name)";

            if (!string.IsNullOrEmpty(queryDto.Group)) where += " and Lower(GroupName) = Lower(@Group)";

            if (!string.IsNullOrEmpty(queryDto.Content)) where += " and Content Like @Content";

            var sqlQuery =
                $"select * from {tableName} where 1=1 {where} order by Added desc limit @Limit offset @Offset";

            var sqlParams = new SpannerParameterCollection()
            {
                { "StatusName", SpannerDbType.String, queryDto.StatusName ?? string.Empty },
                { "GroupName", SpannerDbType.String, queryDto.Group ?? string.Empty },
                { "Name", SpannerDbType.String, queryDto.Name ?? string.Empty },
                { "Content", SpannerDbType.String, $"'%{queryDto.Content}%'" },
                { "Offset", SpannerDbType.Int64, queryDto.CurrentPage * queryDto.PageSize},
                { "Limit", SpannerDbType.Int64, queryDto.PageSize}
            };

            var messages = new List<MessageDto>();
            long count = 0;
            using (var connection = new SpannerConnection(_options.ConnectionString))
            {
                var countCmd = connection.CreateSelectCommand($"select count(1) from {tableName} where 1=1 {where}",
                 new SpannerParameterCollection()
                 {
                    { "StatusName", SpannerDbType.String, queryDto.StatusName ?? string.Empty },
                    { "GroupName", SpannerDbType.String, queryDto.Group ?? string.Empty },
                    { "Name", SpannerDbType.String, queryDto.Name ?? string.Empty },
                    { "Content", SpannerDbType.String, $"'%{queryDto.Content}%'" },
                    { "Offset", SpannerDbType.Int64, queryDto.CurrentPage * queryDto.PageSize},
                    { "Limit", SpannerDbType.Int64, queryDto.PageSize}
                 });
                count = (long)countCmd.ExecuteScalar();

                var cmd = connection.CreateSelectCommand(sqlQuery, sqlParams);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var index = 0;
                    messages.Add(new MessageDto
                    {
                        Id = reader.GetString(index++),
                        Version = reader.GetString(index++),
                        Name = reader.GetString(index++),
                        Group = queryDto.MessageType == MessageType.Subscribe ? reader.GetString(index++) : default,
                        Content = reader.GetString(index++),
                        Retries = reader.GetInt32(index++),
                        Added = reader.GetDateTime(index++),
                        ExpiresAt = reader.GetDateTime(index++),
                        StatusName = reader.GetString(index)
                    });
                }
            }
            return new PagedQueryResult<MessageDto> { Items = messages, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
        }

        public int PublishedFailedCount()
        {
            return GetNumberOfMessage(_pubName, nameof(StatusName.Failed));
        }

        public int PublishedSucceededCount()
        {
            return GetNumberOfMessage(_pubName, nameof(StatusName.Succeeded));
        }

        public int ReceivedFailedCount()
        {
            return GetNumberOfMessage(_recName, nameof(StatusName.Failed));
        }

        public int ReceivedSucceededCount()
        {
            return GetNumberOfMessage(_recName, nameof(StatusName.Succeeded));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return GetHourlyTimelineStats(tableName, nameof(StatusName.Succeeded));
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return GetHourlyTimelineStats(tableName, nameof(StatusName.Failed));
        }

        private int GetNumberOfMessage(string tableName, string statusName)
        {
            var sqlQuery =
                $"select count(Id) from {tableName} where Lower(StatusName) = Lower(@state)";

            var sqlParams = new SpannerParameterCollection()
            {
                { "state", SpannerDbType.String, statusName },
            };

            using var connection = new SpannerConnection(_options.ConnectionString);
            var cmd = connection.CreateSelectCommand(sqlQuery, sqlParams);
            var count = cmd.ExecuteScalar();
            return (int)count;
        }

        private Dictionary<DateTime, int> GetHourlyTimelineStats(string tableName, string statusName)
        {
            var endDate = DateTime.Now;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => x.ToString("yyyy-MM-dd-HH"), x => x);

            return GetTimelineStats(tableName, statusName, keyMaps);
        }

        private Dictionary<DateTime, int> GetTimelineStats(
            string tableName,
            string statusName,
            IDictionary<string, DateTime> keyMaps)
        {
            var sqlQuery =
                $@"
with aggr as (
    select FORMAT_TIMESTAMP('%F-%H', Added, 'UTC') as Key,
    count(Id) as Count
    from {tableName}
        where StatusName = 'Succeeded'
    group by FORMAT_TIMESTAMP('%F-%H', Added, 'UTC')
)
select Key,Count from aggr where Key >= @minKey and Key <= @maxKey";

            var sqlParams = new SpannerParameterCollection()
            {
                { "statusName", SpannerDbType.String, statusName },
                { "minKey", SpannerDbType.String, keyMaps.Keys.Min() },
                { "maxKey", SpannerDbType.String, keyMaps.Keys.Max() },
            };

            Dictionary<string, int> valuesMap = new Dictionary<string, int>();

            using (var connection = new SpannerConnection(_options.ConnectionString))
            {
                var cmd = connection.CreateSelectCommand(sqlQuery, sqlParams);  
                using(var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        valuesMap.Add(reader.GetString(0), reader.GetInt32(1));
                    }
                }
            }

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key))
                    valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, int>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        private async Task<MediumMessage> GetMessageAsync(string tableName, long id)
        {
            var sql = $@"SELECT Id AS DbId, Content, Added, ExpiresAt, Retries FROM {tableName} WHERE Id={id}";

            MediumMessage message = null;
            using var connection = new SpannerConnection(_options.ConnectionString);
            var cmd = connection.CreateSelectCommand(sql);
            using(var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    message = new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Content = reader.GetString(1),
                        Added = reader.GetDateTime(2),
                        ExpiresAt = reader.GetDateTime(3),
                        Retries = reader.GetInt32(4)
                    };
                }
            }

            return message;
        }
    }
}