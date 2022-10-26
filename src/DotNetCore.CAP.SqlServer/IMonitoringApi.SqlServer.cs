// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    internal class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly SqlServerOptions _options;
        private readonly string _pubName;
        private readonly string _recName;

        public SqlServerMonitoringApi(IOptions<SqlServerOptions> options, IStorageInitializer initializer)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
        }

        public async Task<StatisticsDto> GetStatistics()
        {
            var sql = $@"
SELECT
(
    SELECT COUNT(Id) FROM {_pubName} WHERE StatusName = N'Succeeded'
) AS PublishedSucceeded,
(
    SELECT COUNT(Id) FROM {_recName} WHERE StatusName = N'Succeeded'
) AS ReceivedSucceeded,
(
    SELECT COUNT(Id) FROM {_pubName} WHERE StatusName = N'Failed'
) AS PublishedFailed,
(
    SELECT COUNT(Id) FROM {_recName} WHERE StatusName = N'Failed'
) AS ReceivedFailed;";

            await using var connection = new SqlConnection(_options.ConnectionString);
            var statistics = await connection.ExecuteReaderAsync(sql, reader =>
            {
                var statisticsDto = new StatisticsDto();

                while (reader.Read())
                {
                    statisticsDto.PublishedSucceeded = reader.GetInt32(0);
                    statisticsDto.ReceivedSucceeded = reader.GetInt32(1);
                    statisticsDto.PublishedFailed = reader.GetInt32(2);
                    statisticsDto.ReceivedFailed = reader.GetInt32(3);
                }

                return Task.FromResult(statisticsDto);
            });

            return statistics;
        }

        public async Task<IDictionary<DateTime, int>> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Failed));
        }

        public async Task<IDictionary<DateTime, int>> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Succeeded));
        }

        public async Task<PagedQueryResult<MessageDto>> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? _pubName : _recName;
            var where = string.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName)) where += " and statusname=@StatusName";

            if (!string.IsNullOrEmpty(queryDto.Name)) where += " and name=@Name";

            if (!string.IsNullOrEmpty(queryDto.Group)) where += " and [group]=@Group";

            if (!string.IsNullOrEmpty(queryDto.Content)) where += " and content like @Content";

            var sqlQuery2008 =
                $@"select * from 
                (SELECT t.*, ROW_NUMBER() OVER(order by t.Added desc) AS row_number
                    from {tableName} as t
                    where 1=1 {where}) as tbl
                where tbl.row_number between @offset and @offset + @limit";

            var sqlQuery =
                $"select * from {tableName} where 1=1 {where} order by Added desc offset @Offset rows fetch next @Limit rows only";

            object[] sqlParams =
            {
                new SqlParameter("@StatusName", queryDto.StatusName ?? string.Empty),
                new SqlParameter("@Group", queryDto.Group ?? string.Empty),
                new SqlParameter("@Name", queryDto.Name ?? string.Empty),
                new SqlParameter("@Content", $"%{queryDto.Content}%"),
                new SqlParameter("@Offset", queryDto.CurrentPage * queryDto.PageSize),
                new SqlParameter("@Limit", queryDto.PageSize)
            };

            await using var connection = new SqlConnection(_options.ConnectionString);

            var count = await connection.ExecuteScalarAsync<int>($"select count(1) from {tableName} where 1=1 {where}",
                new SqlParameter("@StatusName", queryDto.StatusName ?? string.Empty),
                new SqlParameter("@Group", queryDto.Group ?? string.Empty),
                new SqlParameter("@Name", queryDto.Name ?? string.Empty),
                new SqlParameter("@Content", $"%{queryDto.Content}%"));

            var items = await connection.ExecuteReaderAsync(_options.IsSqlServer2008 ? sqlQuery2008 : sqlQuery, async reader =>
            {
                var messages = new List<MessageDto>();

                while (await reader.ReadAsync())
                {
                    var index = 0;
                    messages.Add(new MessageDto
                    {
                        Id = reader.GetInt64(index++).ToString(),
                        Version = reader.GetString(index++),
                        Name = reader.GetString(index++),
                        Group = queryDto.MessageType == MessageType.Subscribe ? reader.GetString(index++) : default,
                        Content = reader.GetString(index++),
                        Retries = reader.GetInt32(index++),
                        Added = reader.GetDateTime(index++),
                        ExpiresAt = reader.IsDBNull(index++) ? null : reader.GetDateTime(index - 1),
                        StatusName = reader.GetString(index)
                    });
                }

                return messages;
            }, sqlParams);

            return new PagedQueryResult<MessageDto> { Items = items, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
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

        public async Task<MediumMessage?> GetPublishedMessageAsync(long id) => await GetMessageAsync(_pubName, id);

        public async Task<MediumMessage?> GetReceivedMessageAsync(long id) => await GetMessageAsync(_recName, id);

        private async ValueTask<int> GetNumberOfMessage(string tableName, string statusName)
        {
            var sqlQuery =
                $"select count(Id) from {tableName} with (nolock) where StatusName = @state";
            await using var connection = new SqlConnection(_options.ConnectionString);
            return await connection.ExecuteScalarAsync<int>(sqlQuery, new SqlParameter("@state", statusName));
        }

        private Task<Dictionary<DateTime, int>> GetHourlyTimelineStats(string tableName, string statusName)
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

        private async Task<Dictionary<DateTime, int>> GetTimelineStats(
            string tableName,
            string statusName,
            IDictionary<string, DateTime> keyMaps)
        {
            var sqlQuery2008 = $@"
with aggr as (
    select replace(convert(varchar, Added, 111), '/','-') + '-' + CONVERT(varchar, DATEPART(hh, Added)) as [Key],
        count(Id) [Count]
    from  {tableName}
    where StatusName = @statusName
    group by replace(convert(varchar, Added, 111), '/','-') + '-' + CONVERT(varchar, DATEPART(hh, Added))
)
select [Key], [Count] from aggr with (nolock) where [Key] >= @minKey and [Key] <= @maxKey;";

            //SQL Server 2012+ 
            var sqlQuery = $@"
with aggr as (
    select FORMAT(Added,'yyyy-MM-dd-HH') as [Key],
        count(Id) [Count]
    from  {tableName}
    where StatusName = @statusName
    group by FORMAT(Added,'yyyy-MM-dd-HH')
)
select [Key], [Count] from aggr with (nolock) where [Key] >= @minKey and [Key] <= @maxKey;";

            object[] sqlParams =
            {
                new SqlParameter("@statusName", statusName),
                new SqlParameter("@minKey", keyMaps.Keys.Min()),
                new SqlParameter("@maxKey", keyMaps.Keys.Max())
            };

            Dictionary<string, int> valuesMap;
            await using (var connection = new SqlConnection(_options.ConnectionString))
            {
                valuesMap = await connection.ExecuteReaderAsync(_options.IsSqlServer2008 ? sqlQuery2008 : sqlQuery, async reader =>
                {
                    var dictionary = new Dictionary<string, int>();

                    while (await reader.ReadAsync())
                    {
                        dictionary.Add(reader.GetString(0), reader.GetInt32(1));
                    }

                    return dictionary;
                }, sqlParams);
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
            var sql = $@"SELECT TOP 1 Id AS DbId, Content, Added, ExpiresAt, Retries FROM {tableName} WITH (readpast) WHERE Id={id}";

            await using var connection = new SqlConnection(_options.ConnectionString);
            var mediumMessage = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                MediumMessage? message = null;

                while (await reader.ReadAsync())
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

                return message;
            });

            return mediumMessage;
        }
    }
}
