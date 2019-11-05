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
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    internal class MySqlMonitoringApi : IMonitoringApi
    {
        private readonly IOptions<MySqlOptions> _options;
        private readonly string _prefix;

        public MySqlMonitoringApi(IOptions<MySqlOptions> options)
        {
            _options = options;
            _prefix = options.Value.TableNamePrefix ?? throw new ArgumentNullException(nameof(options));
        }

        public StatisticsDto GetStatistics()
        {
            var sql = string.Format(@"
set transaction isolation level read committed;
select count(Id) from `{0}.published` where StatusName = N'Succeeded';
select count(Id) from `{0}.received` where StatusName = N'Succeeded';
select count(Id) from `{0}.published` where StatusName = N'Failed';
select count(Id) from `{0}.received` where StatusName = N'Failed';", _prefix);

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

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "published" : "received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName,nameof(StatusName.Failed)));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "published" : "received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, nameof( StatusName.Succeeded)));
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? "published" : "received";
            var where = string.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
            {
                where += " and StatusName=@StatusName";
            }

            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                where += " and Name=@Name";
            }

            if (!string.IsNullOrEmpty(queryDto.Group))
            {
                where += " and `Group`=@Group";
            }

            if (!string.IsNullOrEmpty(queryDto.Content))
            {
                where += " and Content like '%@Content%'";
            }

            var sqlQuery =
                $"select * from `{_prefix}.{tableName}` where 1=1 {where} order by Added desc limit @Limit offset @Offset";

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
            return UseConnection(conn => GetNumberOfMessage(conn, "published", nameof( StatusName.Failed)));
        }

        public int PublishedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "published", nameof(StatusName.Succeeded)));
        }

        public int ReceivedFailedCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", nameof(StatusName.Failed)));
        }

        public int ReceivedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", nameof(StatusName.Succeeded)));
        }

        private int GetNumberOfMessage(IDbConnection connection, string tableName, string statusName)
        {
            var sqlQuery = $"select count(Id) from `{_prefix}.{tableName}` where StatusName = @state";

            var count = connection.ExecuteScalar<int>(sqlQuery, new { state = statusName });
            return count;
        }

        private T UseConnection<T>(Func<IDbConnection, T> action)
        {
            return action(new MySqlConnection(_options.Value.ConnectionString));
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
select aggr.* from (
    select date_format(`Added`,'%Y-%m-%d-%H') as `Key`,
        count(id) `Count`
    from  `{_prefix}.{tableName}`
    where StatusName = @statusName
    group by date_format(`Added`,'%Y-%m-%d-%H')
) aggr where `Key` in @keys;";

            var valuesMap = connection.Query<TimelineCounter>(
                    sqlQuery,
                    new { keys = keyMaps.Keys, statusName })
                .ToDictionary(x => x.Key, x => x.Count);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key))
                {
                    valuesMap.Add(key, 0);
                }
            }

            var result = new Dictionary<DateTime, int>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        public async Task<MediumMessage> GetPublishedMessageAsync(long id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.published` WHERE `Id`={id};";

            await using var connection = new MySqlConnection(_options.Value.ConnectionString);
            return await connection.QueryFirstOrDefaultAsync<MediumMessage>(sql);
        }

        public async Task<MediumMessage> GetReceivedMessageAsync(long id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.received` WHERE Id={id};";
            await using var connection = new MySqlConnection(_options.Value.ConnectionString);
            return await connection.QueryFirstOrDefaultAsync<MediumMessage>(sql);
        }
    }

    class TimelineCounter
    {
        public string Key { get; set; }
        public int Count { get; set; }
    }
}