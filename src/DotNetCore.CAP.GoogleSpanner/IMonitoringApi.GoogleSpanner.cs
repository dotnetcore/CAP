using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Google.Cloud.Spanner.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Spanner
{
    internal class GoogleSpannerMonitoringApi : IMonitoringApi
    {
        private readonly GoogleSpannerOptions _options;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly ISerializer _serializer;

        public GoogleSpannerMonitoringApi(
            IOptions<GoogleSpannerOptions> options,
            IStorageInitializer initializer,
            ISerializer serializer)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
            _serializer = serializer;
        }

        public async Task<MediumMessage?> GetPublishedMessageAsync(long id) => await GetMessageAsync(_pubName, id).ConfigureAwait(false);

        public async Task<MediumMessage?> GetReceivedMessageAsync(long id) => await GetMessageAsync(_recName, id).ConfigureAwait(false);

        public async Task<StatisticsDto> GetStatisticsAsync()
        {
            var sql = $@"
                        SELECT
                        (
                            SELECT COUNT(Id) FROM `{_pubName}` WHERE StatusName = '{StatusName.Succeeded}'
                        ) AS PublishedSucceeded,
                        (
                            SELECT COUNT(Id) FROM `{_recName}` WHERE StatusName = '{StatusName.Succeeded}'
                        ) AS ReceivedSucceeded,
                        (
                            SELECT COUNT(Id) FROM `{_pubName}` WHERE StatusName = '{StatusName.Failed}'
                        ) AS PublishedFailed,
                        (
                            SELECT COUNT(Id) FROM `{_recName}` WHERE StatusName = '{StatusName.Failed}'
                        ) AS ReceivedFailed";

            StatisticsDto statisticsDto = new();
            var connection = new SpannerConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var cmd = connection.CreateSelectCommand(sql);
            var reader = await cmd.ExecuteReaderAsync()
                                    .ConfigureAwait(false);

            while (await reader.ReadAsync())
            {
                statisticsDto.PublishedSucceeded = reader.GetInt32(0);
                statisticsDto.ReceivedSucceeded = reader.GetInt32(1);
                statisticsDto.PublishedFailed = reader.GetInt32(2);
                statisticsDto.ReceivedFailed = reader.GetInt32(3);
            }

            return statisticsDto;
        }

        public async Task<PagedQueryResult<MessageDto>> GetMessagesAsync(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? _pubName : _recName;
            var where = string.Empty;

            if (!string.IsNullOrEmpty(queryDto.StatusName)) where += " and Lower(`StatusName`) = Lower(@StatusName)";

            if (!string.IsNullOrEmpty(queryDto.Name)) where += " and Lower(`Name`) = Lower(@Name)";

            if (!string.IsNullOrEmpty(queryDto.Group)) where += " and Lower(`Group`) = Lower(@Group)";

            if (!string.IsNullOrEmpty(queryDto.Content)) where += " and `Content` Like @Content";

            var sqlQuery =
                $"select * from {tableName} where 1=1 {where} order by Added desc limit @Limit offset @Offset";

            var messages = new List<MessageDto>();

            var connection = new SpannerConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var countCmd = connection.CreateSelectCommand(
                $"select count(1) from `{tableName}` where 1=1 {where}",
                 new SpannerParameterCollection()
                 {
                        { "StatusName", SpannerDbType.String, queryDto.StatusName ?? string.Empty },
                        { "GroupName", SpannerDbType.String, queryDto.Group ?? string.Empty },
                        { "Name", SpannerDbType.String, queryDto.Name ?? string.Empty },
                        { "Content", SpannerDbType.String, $"'%{queryDto.Content}%'" },
                        { "Offset", SpannerDbType.Int64, queryDto.CurrentPage * queryDto.PageSize},
                        { "Limit", SpannerDbType.Int64, queryDto.PageSize}
                 });

            var count = await countCmd.ExecuteScalarAsync<int>()
                                        .ConfigureAwait(false);

            var cmd = connection.CreateSelectCommand(
                sqlQuery,
                new SpannerParameterCollection()
                    {
                        { "StatusName", SpannerDbType.String, queryDto.StatusName ?? string.Empty },
                        { "GroupName", SpannerDbType.String, queryDto.Group ?? string.Empty },
                        { "Name", SpannerDbType.String, queryDto.Name ?? string.Empty },
                        { "Content", SpannerDbType.String, $"'%{queryDto.Content}%'" },
                        { "Offset", SpannerDbType.Int64, queryDto.CurrentPage * queryDto.PageSize},
                        { "Limit", SpannerDbType.Int64, queryDto.PageSize}
                    });

            using var reader = await cmd.ExecuteReaderAsync()
                                        .ConfigureAwait(false);
            while (await reader.ReadAsync()
                        .ConfigureAwait(false))
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

            return new PagedQueryResult<MessageDto> { Items = messages, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
        }

        public ValueTask<int> PublishedFailedCount()
        {
            return GetNumberOfMessage(_pubName, nameof(StatusName.Failed));
        }

        public ValueTask<int> PublishedSucceededCount()
        {
            return GetNumberOfMessage(_pubName, nameof(StatusName.Succeeded));
        }

        public ValueTask<int> ReceivedFailedCount()
        {
            return GetNumberOfMessage(_recName, nameof(StatusName.Failed));
        }

        public ValueTask<int> ReceivedSucceededCount()
        {
            return GetNumberOfMessage(_recName, nameof(StatusName.Succeeded));
        }

        public async Task<IDictionary<DateTime, int>> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Succeeded));
        }

        public async Task<IDictionary<DateTime, int>> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Failed));
        }

        private async ValueTask<int> GetNumberOfMessage(string tableName, string statusName)
        {
            var sqlQuery =
                $"SELECT COUNT(`Id`) FROM `{tableName}` WHERE LOWER(StatusName) = LOWER(@state)";

            var sqlParams = new SpannerParameterCollection()
            {
                { "state", SpannerDbType.String, statusName },
            };

            var connection = new SpannerConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var cmd = connection.CreateSelectCommand(sqlQuery, sqlParams);
            return await cmd.ExecuteScalarAsync<int>()
                .ConfigureAwait(false);
        }

        private Task<Dictionary<DateTime, int>> GetHourlyTimelineStats(string tableName, string statusName)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => x.ToString("yyyy-MM-dd-HH"), x => x);

            return GetTimelineStats(tableName, statusName, keyMaps);
        }

        private async Task<Dictionary<DateTime, int>> GetTimelineStats(
            string tableName,
            string statusName,
            IDictionary<string, DateTime> keyMaps)
        {
            var sqlQuery =
                $@"
                    WITH aggr AS (
                        SELECT FORMAT_TIMESTAMP('%F-%H', `Added`, 'UTC') AS `Key`,
                        COUNT(Id) AS `Count`
                        FROM `{tableName}`
                            WHERE `StatusName` = 'Succeeded'
                        GROUP BY FORMAT_TIMESTAMP('%F-%H', `Added`, 'UTC')
                    )
                    SELECT `Key`,`Count` from `aggr` where `Key` >= '@minKey' and `Key` <= '@maxKey'";

            var sqlParams = new SpannerParameterCollection()
            {
                { "statusName", SpannerDbType.String, statusName },
                { "minKey", SpannerDbType.String, keyMaps.Keys.Min() },
                { "maxKey", SpannerDbType.String, keyMaps.Keys.Max() },
            };

            Dictionary<string, int> valuesMap = new();

            var connection = new SpannerConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var cmd = connection.CreateSelectCommand(sqlQuery, sqlParams);

            using var reader = await cmd.ExecuteReaderAsync()
                                         .ConfigureAwait(false);
            
            while (await reader.ReadAsync()
                                .ConfigureAwait(false))
            {
                valuesMap.Add(reader.GetString(0), reader.GetInt32(1));
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

        private async Task<MediumMessage?> GetMessageAsync(string tableName, long id)
        {
            var sql = $@"SELECT `Id` AS `DbId`, `Content`, `Added`, `ExpiresAt`, `Retries` FROM `{tableName}` WHERE `Id`='{id}'";

            using var connection = new SpannerConnection(_options.ConnectionString);
            var cmd = connection.CreateSelectCommand(sql);
            await using var _ = connection.ConfigureAwait(false);
            using var reader = await cmd.ExecuteReaderAsync()
                                         .ConfigureAwait(false);
            while (await reader.ReadAsync()
                               .ConfigureAwait(false))
            {
                return new MediumMessage
                {
                    DbId = reader.GetInt64(0).ToString(),
                    Origin = _serializer.Deserialize(reader.GetString(1))!,
                    Content = reader.GetString(1),
                    Added = reader.GetDateTime(2),
                    ExpiresAt = reader.GetDateTime(3),
                    Retries = reader.GetInt32(4)
                };
            }

            return null;
        }
    }
}