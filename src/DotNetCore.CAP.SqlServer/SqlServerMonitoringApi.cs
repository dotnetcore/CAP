using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer
{
    internal class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly SqlServerOptions _options;
        private readonly SqlServerStorage _storage;

        public SqlServerMonitoringApi(IStorage storage, SqlServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storage = storage as SqlServerStorage ?? throw new ArgumentNullException(nameof(storage));
        }

        public StatisticsDto GetStatistics()
        {
            var sql = string.Format(@"
set transaction isolation level read committed;
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Succeeded';
select count(Id) from [{0}].Received with (nolock) where StatusName = N'Succeeded';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Failed';
select count(Id) from [{0}].Received with (nolock) where StatusName = N'Failed';
select count(Id) from [{0}].Published with (nolock) where StatusName in (N'Processing',N'Scheduled',N'Enqueued');
select count(Id) from [{0}].Received with (nolock) where StatusName in (N'Processing',N'Scheduled',N'Enqueued');",
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
            return statistics;
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "Published" : "Received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, StatusName.Failed));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "Published" : "Received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, StatusName.Succeeded));
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? "Published" : "Received";
            var where = string.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
                if (string.Equals(queryDto.StatusName, StatusName.Processing,
                    StringComparison.CurrentCultureIgnoreCase))
                    where += " and statusname in (N'Processing',N'Scheduled',N'Enqueued')";
                else
                    where += " and statusname=@StatusName";
            if (!string.IsNullOrEmpty(queryDto.Name))
                where += " and name=@Name";
            if (!string.IsNullOrEmpty(queryDto.Group))
                where += " and group=@Group";
            if (!string.IsNullOrEmpty(queryDto.Content))
                where += " and content like '%@Content%'";

            var sqlQuery =
                $"select * from [{_options.Schema}].{tableName} where 1=1 {where} order by Added desc offset @Offset rows fetch next @Limit rows only";

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
            return UseConnection(conn => GetNumberOfMessage(conn, "Published", StatusName.Failed));
        }

        public int PublishedProcessingCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "Published", StatusName.Processing));
        }

        public int PublishedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "Published", StatusName.Succeeded));
        }

        public int ReceivedFailedCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "Received", StatusName.Failed));
        }

        public int ReceivedProcessingCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "Received", StatusName.Processing));
        }

        public int ReceivedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "Received", StatusName.Succeeded));
        }

        private int GetNumberOfMessage(IDbConnection connection, string tableName, string statusName)
        {
            var sqlQuery = statusName == StatusName.Processing
                ? $"select count(Id) from [{_options.Schema}].{tableName} with (nolock) where StatusName in (N'Processing',N'Scheduled',N'Enqueued')"
                : $"select count(Id) from [{_options.Schema}].{tableName} with (nolock) where StatusName = @state";

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
            //SQL Server 2012+
            var sqlQuery =
                $@"
with aggr as (
    select FORMAT(Added,'yyyy-MM-dd-HH') as [Key],
        count(id) [Count]
    from  [{_options.Schema}].{tableName}
    where StatusName = @statusName
    group by FORMAT(Added,'yyyy-MM-dd-HH')
)
select [Key], [Count] from aggr with (nolock) where [Key] in @keys;";

            var valuesMap = connection.Query(
                    sqlQuery,
                    new {keys = keyMaps.Keys, statusName})
                .ToDictionary(x => (string) x.Key, x => (int) x.Count);

            foreach (var key in keyMaps.Keys)
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);

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