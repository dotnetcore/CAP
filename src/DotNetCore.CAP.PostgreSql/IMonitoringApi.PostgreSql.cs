// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlMonitoringApi : IMonitoringApi
    {
        private readonly PostgreSqlOptions _options;
        private readonly PostgreSqlStorage _storage;

        public PostgreSqlMonitoringApi(IStorage storage, IOptions<PostgreSqlOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _storage = storage as PostgreSqlStorage ?? throw new ArgumentNullException(nameof(storage));
        }

        public StatisticsDto GetStatistics()
        {
            var sql = string.Format(@"
select count(""Id"") from ""{0}"".""published"" where ""StatusName"" = N'Succeeded';
select count(""Id"") from ""{0}"".""received""  where ""StatusName"" = N'Succeeded';
select count(""Id"") from ""{0}"".""published"" where ""StatusName"" = N'Failed';
select count(""Id"") from ""{0}"".""received""  where ""StatusName"" = N'Failed';",
                _options.Schema);

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
            var tableName = queryDto.MessageType == MessageType.Publish ? "published" : "received";
            var where = string.Empty;

            if (!string.IsNullOrEmpty(queryDto.StatusName))
            {
                where += " and Lower(\"StatusName\") = Lower(@StatusName)";
            }

            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                where += " and Lower(\"Name\") = Lower(@Name)";
            }

            if (!string.IsNullOrEmpty(queryDto.Group))
            {
                where += " and Lower(\"Group\") = Lower(@Group)";
            }

            if (!string.IsNullOrEmpty(queryDto.Content))
            {
                where += " and \"Content\" ILike '%@Content%'";
            }

            var sqlQuery =
                $"select * from \"{_options.Schema}\".\"{tableName}\" where 1=1 {where} order by \"Added\" desc offset @Offset limit @Limit";

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
            return UseConnection(conn => GetNumberOfMessage(conn, "published", StatusName.Failed));
        }

        public int PublishedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "published", StatusName.Succeeded));
        }

        public int ReceivedFailedCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", StatusName.Failed));
        }

        public int ReceivedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", StatusName.Succeeded));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "published" : "received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, StatusName.Succeeded));
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "published" : "received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, StatusName.Failed));
        }

        private int GetNumberOfMessage(IDbConnection connection, string tableName, string statusName)
        {
            var sqlQuery =
                $"select count(\"Id\") from \"{_options.Schema}\".\"{tableName}\" where Lower(\"StatusName\") = Lower(@state)";

            var count = connection.ExecuteScalar<int>(sqlQuery, new {state = statusName});
            return count;
        }

        private T UseConnection<T>(Func<IDbConnection, T> action)
        {
            return _storage.UseConnection(action);
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
    from ""{_options.Schema}"".""{tableName}""
        where ""StatusName"" = @statusName
    group by to_char(""Added"", 'yyyy-MM-dd-HH')
)
select ""Key"",""Count"" from aggr where ""Key""= Any(@keys);";

            var valuesMap = connection.Query<TimelineCounter>(sqlQuery, new {keys = keyMaps.Keys.ToList(), statusName})
                .ToList()
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
    }
}