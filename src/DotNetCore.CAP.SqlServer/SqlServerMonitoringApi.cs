using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.SqlServer
{
    internal class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly SqlServerStorage _storage;
        private readonly SqlServerOptions _options;

        public SqlServerMonitoringApi(IStorage storage, SqlServerOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;
            _storage = storage as SqlServerStorage;
        }

        public IDictionary<DateTime, int> FailedByDatesCount()
        {
            return new Dictionary<DateTime, int>();
        }

        public StatisticsDto GetStatistics()
        {
            string sql = String.Format(@"
set transaction isolation level read committed;
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Succeeded';
select count(Id) from [{0}].Received with (nolock) where StatusName = N'Succeeded';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Failed';
select count(Id) from [{0}].Received with (nolock) where StatusName = N'Failed';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Processing';
select count(Id) from [{0}].Received with (nolock) where StatusName = N'Processing';",
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

                    stats.PublishedProcessing = multi.ReadSingle<int>();
                    stats.ReceivedProcessing = multi.ReadSingle<int>();
                }
                return stats;
            });
            statistics.Servers = 1;
            return statistics;
        }

        public IDictionary<DateTime, int> HourlyFailedJobs()
        {
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, "failed"));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs()
        {
            return UseConnection(connection =>
                 GetHourlyTimelineStats(connection, "succeeded"));
        }

        public IList<ServerDto> Servers()
        {
            return new List<ServerDto>();
        }

        public IDictionary<DateTime, int> SucceededByDatesCount()
        {
            return new Dictionary<DateTime, int>();
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == Models.MessageType.Publish ? "Published" : "Received";
            var where = string.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
            {
                where += " and statusname=@StatusName";
            }
            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                where += " and name=@Name";
            }
            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                where += " and group=@Group";
            }
            if (!string.IsNullOrEmpty(queryDto.Content))
            {
                where += " and content like '%@Content%'";
            }

            var sqlQuery = $"select * from [{_options.Schema}].{tableName} where 1=1 {where} order by Added desc offset @Offset rows fetch next @Limit rows only";

            return UseConnection(conn =>
            {
                return conn.Query<MessageDto>(sqlQuery, new
                {
                    StatusName = queryDto.StatusName,
                    Group = queryDto.Group,
                    Name = queryDto.Name,
                    Content = queryDto.Content,
                    Offset = queryDto.CurrentPage * queryDto.PageSize,
                    Limit = queryDto.PageSize,
                }).ToList();
            });
        }

        public int PublishedFailedCount()
        {
            return UseConnection(conn =>
            {
                return GetNumberOfMessage(conn, "Published", StatusName.Failed);
            });
        }

        public int PublishedProcessingCount()
        {
            return UseConnection(conn =>
            {
                return GetNumberOfMessage(conn, "Published", StatusName.Processing);
            });
        }

        public int PublishedSucceededCount()
        {
            return UseConnection(conn =>
            {
                return GetNumberOfMessage(conn, "Published", StatusName.Succeeded);
            });
        }

        public int ReceivedFailedCount()
        {
            return UseConnection(conn =>
            {
                return GetNumberOfMessage(conn, "Received", StatusName.Failed);
            });
        }

        public int ReceivedProcessingCount()
        {
            return UseConnection(conn =>
            {
                return GetNumberOfMessage(conn, "Received", StatusName.Processing);
            });
        }

        public int ReceivedSucceededCount()
        {
            return UseConnection(conn =>
            {
                return GetNumberOfMessage(conn, "Received", StatusName.Succeeded);
            });
        }

        private int GetNumberOfMessage(IDbConnection connection, string tableName, string statusName)
        {
            var sqlQuery = $"select count(Id) from [{_options.Schema}].{tableName} with (nolock) where StatusName = @state";
            var count = connection.ExecuteScalar<int>(sqlQuery, new { state = statusName });
            return count;
        }

        private T UseConnection<T>(Func<IDbConnection, T> action)
        {
            return _storage.UseConnection(action);
        }

        private Dictionary<DateTime, int> GetHourlyTimelineStats(IDbConnection connection, string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd-HH")}", x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, int> GetTimelineStats(IDbConnection connection, string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd")}", x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, int> GetTimelineStats(
           IDbConnection connection,
           IDictionary<string, DateTime> keyMaps)
        {
            string sqlQuery =
$@"select [Key], [Value] as [Count] from [{_options.Schema}].AggregatedCounter with (nolock)
where [Key] in @keys";

            var valuesMap = connection.Query(
                sqlQuery,
                new { keys = keyMaps.Keys })
                .ToDictionary(x => (string)x.Key, x => (int)x.Count);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
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