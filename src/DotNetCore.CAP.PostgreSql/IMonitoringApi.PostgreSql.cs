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
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlMonitoringApi : IMonitoringApi
    {
        private readonly PostgreSqlOptions _options;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly ISerializer _serializer;

        public PostgreSqlMonitoringApi(IOptions<PostgreSqlOptions> options, IStorageInitializer initializer, ISerializer serializer)
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
    SELECT COUNT(""Id"") FROM {_pubName} WHERE ""StatusName"" = N'Succeeded'
) AS ""PublishedSucceeded"",
(
    SELECT COUNT(""Id"") FROM {_recName} WHERE ""StatusName"" = N'Succeeded'
) AS ""ReceivedSucceeded"",
(
    SELECT COUNT(""Id"") FROM {_pubName} WHERE ""StatusName"" = N'Failed'
) AS ""PublishedFailed"",
(
    SELECT COUNT(""Id"") FROM {_recName} WHERE ""StatusName"" = N'Failed'
) AS ""ReceivedFailed"",
(
    SELECT COUNT(""Id"") FROM {_pubName} WHERE ""StatusName"" = N'Delayed'
) AS ""PublishedDelayed"";";

            var connection = new NpgsqlConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var statistics = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                var statisticsDto = new StatisticsDto();

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    statisticsDto.PublishedSucceeded = reader.GetInt32(0);
                    statisticsDto.ReceivedSucceeded = reader.GetInt32(1);
                    statisticsDto.PublishedFailed = reader.GetInt32(2);
                    statisticsDto.ReceivedFailed = reader.GetInt32(3);
                    statisticsDto.PublishedDelayed = reader.GetInt32(4);
                }

                return statisticsDto;
            }).ConfigureAwait(false);

            return statistics;
        }

        public async Task<PagedQueryResult<MessageDto>> GetMessagesAsync(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? _pubName : _recName;
            var where = string.Empty;

            if (!string.IsNullOrEmpty(queryDto.StatusName)) where += " and Lower(\"StatusName\") = Lower(@StatusName)";

            if (!string.IsNullOrEmpty(queryDto.Name)) where += " and Lower(\"Name\") = Lower(@Name)";

            if (!string.IsNullOrEmpty(queryDto.Group)) where += " and Lower(\"Group\") = Lower(@Group)";

            if (!string.IsNullOrEmpty(queryDto.Content)) where += " and \"Content\" ILike @Content";

            var sqlQuery =
                $"select * from {tableName} where 1=1 {where} order by \"Added\" desc offset @Offset limit @Limit";

            var connection = new NpgsqlConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var count = await connection.ExecuteScalarAsync<int>($"select count(1) from {tableName} where 1=1 {where}",
                new NpgsqlParameter("@StatusName", queryDto.StatusName ?? string.Empty),
                new NpgsqlParameter("@Group", queryDto.Group ?? string.Empty),
                new NpgsqlParameter("@Name", queryDto.Name ?? string.Empty),
                new NpgsqlParameter("@Content", $"%{queryDto.Content}%")).ConfigureAwait(false);

            object[] sqlParams =
            {
                new NpgsqlParameter("@StatusName", queryDto.StatusName ?? string.Empty),
                new NpgsqlParameter("@Group", queryDto.Group ?? string.Empty),
                new NpgsqlParameter("@Name", queryDto.Name ?? string.Empty),
                new NpgsqlParameter("@Content", $"%{queryDto.Content}%"),
                new NpgsqlParameter("@Offset", queryDto.CurrentPage * queryDto.PageSize),
                new NpgsqlParameter("@Limit", queryDto.PageSize)
            };

            var items = await connection.ExecuteReaderAsync(sqlQuery, async reader =>
            {
                var messages = new List<MessageDto>();

                while (await reader.ReadAsync().ConfigureAwait(false))
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
            }, sqlParams: sqlParams).ConfigureAwait(false);

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

        public async Task<IDictionary<DateTime, int>> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Succeeded)).ConfigureAwait(false);
        }

        public async Task<IDictionary<DateTime, int>> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Failed)).ConfigureAwait(false);
        }

        private async ValueTask<int> GetNumberOfMessage(string tableName, string statusName)
        {
            var sqlQuery =
                $"select count(\"Id\") from {tableName} where Lower(\"StatusName\") = Lower(@state)";

            var connection = new NpgsqlConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            return await connection.ExecuteScalarAsync<int>(sqlQuery, new NpgsqlParameter("@state", statusName)).ConfigureAwait(false);
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
            var sqlQuery =
                $@"
with aggr as (
    select to_char(""Added"",'yyyy-MM-dd-HH') as ""Key"",
    count(""Id"") as ""Count""
    from {tableName}
        where ""StatusName"" = @statusName
    group by to_char(""Added"", 'yyyy-MM-dd-HH')
)
select ""Key"",""Count"" from aggr where ""Key"" >= @minKey and ""Key"" <= @maxKey;";

            object[] sqlParams =
            {
                new NpgsqlParameter("@statusName", statusName),
                new NpgsqlParameter("@minKey", keyMaps.Keys.Min()),
                new NpgsqlParameter("@maxKey", keyMaps.Keys.Max())
            };

            Dictionary<string, int> valuesMap;
            var connection = new NpgsqlConnection(_options.ConnectionString);
            await using (connection.ConfigureAwait(false))
            {
                valuesMap = await connection.ExecuteReaderAsync(sqlQuery, async reader =>
                {
                    var dictionary = new Dictionary<string, int>();

                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        dictionary.Add(reader.GetString(0), reader.GetInt32(1));
                    }

                    return dictionary;
                }, sqlParams: sqlParams).ConfigureAwait(false);
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
            var sql = $@"SELECT ""Id"" AS ""DbId"", ""Content"", ""Added"", ""ExpiresAt"", ""Retries"" FROM {tableName} WHERE ""Id""={id} FOR UPDATE SKIP LOCKED";

            var connection = new NpgsqlConnection(_options.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var mediumMessage = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                MediumMessage? message = null;

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    message = new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = _serializer.Deserialize(reader.GetString(1))!,
                        Content = reader.GetString(1),
                        Added = reader.GetDateTime(2),
                        ExpiresAt = reader.GetDateTime(3),
                        Retries = reader.GetInt32(4)
                    };
                }

                return message;
            }).ConfigureAwait(false);

            return mediumMessage;
        }
    }
}