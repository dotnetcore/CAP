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

namespace DotNetCore.CAP.PostgreSql;

public class PostgreSqlMonitoringApi : IMonitoringApi
{
    private readonly PostgreSqlOptions _options;
    private readonly string _pubName;
    private readonly string _recName;
    private readonly ISerializer _serializer;

    public PostgreSqlMonitoringApi(IOptions<PostgreSqlOptions> options, IStorageInitializer initializer,
        ISerializer serializer)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _pubName = initializer.GetPublishedTableName();
        _recName = initializer.GetReceivedTableName();
        _serializer = serializer;
    }

    public async Task<MediumMessage?> GetPublishedMessageAsync(long id)
    {
        return await GetMessageAsync(_pubName, id).ConfigureAwait(false);
    }

    public async Task<MediumMessage?> GetReceivedMessageAsync(long id)
    {
        return await GetMessageAsync(_recName, id).ConfigureAwait(false);
    }

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

        var connection = _options.CreateConnection();
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

        if (!string.IsNullOrEmpty(queryDto.StatusName)) where += " AND Lower(\"StatusName\") = Lower(@StatusName)";

        if (!string.IsNullOrEmpty(queryDto.Name)) where += " AND Lower(\"Name\") = Lower(@Name)";

        if (!string.IsNullOrEmpty(queryDto.Group)) where += " AND Lower(\"Group\") = Lower(@Group)";

        if (!string.IsNullOrEmpty(queryDto.Content)) where += " AND \"Content\" ILike @Content";

        var sqlQuery =
            $"SELECT * FROM {tableName} WHERE 1=1 {where} ORDER BY \"Added\" DESC OFFSET @Offset LIMIT @Limit";

        var connection = _options.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);

        var count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM {tableName} WHERE 1=1 {where}",
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

        return new PagedQueryResult<MessageDto>
            { Items = items, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
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
            $"SELECT COUNT(\"Id\") FROM {tableName} WHERE Lower(\"StatusName\") = Lower(@State)";

        var connection = _options.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>(sqlQuery, new NpgsqlParameter("@State", statusName))
            .ConfigureAwait(false);
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
WITH Aggr AS (
    SELECT to_char(""Added"",'yyyy-MM-dd-HH') AS ""Key"",
    COUNT(""Id"") AS ""Count""
    FROM {tableName}
        WHERE ""StatusName"" = @StatusName
    GROUP BY to_char(""Added"", 'yyyy-MM-dd-HH')
)
SELECT ""Key"",""Count"" from Aggr WHERE ""Key"" >= @MinKey AND ""Key"" <= @MaxKey;";

        object[] sqlParams =
        {
            new NpgsqlParameter("@StatusName", statusName),
            new NpgsqlParameter("@MinKey", keyMaps.Keys.Min()),
            new NpgsqlParameter("@MaxKey", keyMaps.Keys.Max())
        };

        Dictionary<string, int> valuesMap;
        var connection = _options.CreateConnection();
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
            valuesMap.TryAdd(key, 0);
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
        var sql =
            $@"SELECT ""Id"" AS ""DbId"", ""Content"", ""Added"", ""ExpiresAt"", ""Retries"" FROM {tableName} WHERE ""Id""={id} FOR UPDATE SKIP LOCKED";

        var connection = _options.CreateConnection();
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