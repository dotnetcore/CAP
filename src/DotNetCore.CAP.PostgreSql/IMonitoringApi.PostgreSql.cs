// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlMonitoringApi : IMonitoringApi
    {
        private readonly IOptions<PostgreSqlOptions> _options;
        private readonly string _pubName;
        private readonly string _recName;

        public PostgreSqlMonitoringApi(IOptions<PostgreSqlOptions> options,IStorageInitializer initializer)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
        }

        public async Task<MediumMessage> GetPublishedMessageAsync(long id)
        {
            var sql =
                $"SELECT \"Id\" AS \"DbId\",* FROM {_pubName} WHERE \"Id\"={id} FOR UPDATE SKIP LOCKED";

            using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            return await connection.QueryFirstOrDefaultAsync<MediumMessage>(sql);
        }

        public async Task<MediumMessage> GetReceivedMessageAsync(long id)
        {
            var sql =
                $"SELECT \"Id\" AS \"DbId\",* FROM {_recName} WHERE \"Id\"={id} FOR UPDATE SKIP LOCKED";
            using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            return await connection.QueryFirstOrDefaultAsync<MediumMessage>(sql);
        }

        public StatisticsDto GetStatistics()
        {
            var sql = $@"
select count(""Id"") from {_pubName} where ""StatusName"" = N'Succeeded';
select count(""Id"") from {_recName} where ""StatusName"" = N'Succeeded';
select count(""Id"") from {_pubName} where ""StatusName"" = N'Failed';
select count(""Id"") from {_recName} where ""StatusName"" = N'Failed';";

            var statistics = UseConnection(connection =>
            {
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(sql))
                {
                    stats.PublishedSucceeded = multi.ReadSingle<int>();
                    stats.ReceivedSucceeded = multi.ReadSingle<int>();

                    stats.PublishedFailed = multi.ReadSingle<int>();
                    stats.ReceivedFailed = multi.ReadSingle<int>();
                }

                return stats;
            });
            return statistics;
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? _pubName : _recName;
            var where = string.Empty;

            if (!string.IsNullOrEmpty(queryDto.StatusName)) where += " and Lower(\"StatusName\") = Lower(@StatusName)";

            if (!string.IsNullOrEmpty(queryDto.Name)) where += " and Lower(\"Name\") = Lower(@Name)";

            if (!string.IsNullOrEmpty(queryDto.Group)) where += " and Lower(\"Group\") = Lower(@Group)";

            if (!string.IsNullOrEmpty(queryDto.Content)) where += " and \"Content\" ILike concat('%',@Content,'%')";

            var sqlQuery =
                $"select * from {tableName} where 1=1 {where} order by \"Added\" desc offset @Offset limit @Limit";

            return UseConnection(conn => conn.Query<MessageDto>(sqlQuery, new
            {
                queryDto.StatusName,
                queryDto.Group,
                queryDto.Name,
                queryDto.Content,
                Offset = queryDto.CurrentPage * queryDto.PageSize,
                Limit = queryDto.PageSize
            }).ToList());
        }

        public int PublishedFailedCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, _pubName, nameof(StatusName.Failed)));
        }

        public int PublishedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, _pubName, nameof(StatusName.Succeeded)));
        }

        public int ReceivedFailedCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, _recName, nameof(StatusName.Failed)));
        }

        public int ReceivedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, _recName, nameof(StatusName.Succeeded)));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, nameof(StatusName.Succeeded)));
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, nameof(StatusName.Failed)));
        }

        private int GetNumberOfMessage(IDbConnection connection, string tableName, string statusName)
        {
            var sqlQuery =
                $"select count(\"Id\") from {tableName} where Lower(\"StatusName\") = Lower(@state)";

            var count = connection.ExecuteScalar<int>(sqlQuery, new { state = statusName });
            return count;
        }

        private T UseConnection<T>(Func<IDbConnection, T> action)
        {
            return action(new NpgsqlConnection(_options.Value.ConnectionString));
        }

        private Dictionary<DateTime, int> GetHourlyTimelineStats(IDbConnection connection, string tableName,
            string statusName)
        {
            var endDate = DateTime.Now;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => x.ToString("yyyy-MM-dd-HH"), x => x);

            return GetTimelineStats(connection, tableName, statusName, keyMaps);
        }

        private Dictionary<DateTime, int> GetTimelineStats(
            IDbConnection connection,
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
select ""Key"",""Count"" from aggr where ""Key""= Any(@keys);";

            var valuesMap = connection.Query<TimelineCounter>(sqlQuery, new { keys = keyMaps.Keys.ToList(), statusName })
                .ToList()
                .ToDictionary(x => x.Key, x => x.Count);

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
    }

    internal class TimelineCounter
    {
        public string Key { get; set; }
        public int Count { get; set; }
    }
}