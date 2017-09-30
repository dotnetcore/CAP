using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.MySql
{
    internal class MySqlMonitoringApi : IMonitoringApi
    {
        private readonly string _prefix;
        private readonly MySqlStorage _storage;

        public MySqlMonitoringApi(IStorage storage, MySqlOptions options)
        {
            _storage = storage as MySqlStorage ?? throw new ArgumentNullException(nameof(storage));
            _prefix = options?.TableNamePrefix ?? throw new ArgumentNullException(nameof(options));
        }

        public StatisticsDto GetStatistics()
        {
            var sql = string.Format(@"
set transaction isolation level read committed;
select count(Id) from `{0}.published` where StatusName = N'Succeeded';
select count(Id) from `{0}.received` where StatusName = N'Succeeded';
select count(Id) from `{0}.published` where StatusName = N'Failed';
select count(Id) from `{0}.received` where StatusName = N'Failed';
select count(Id) from `{0}.published` where StatusName in (N'Processing',N'Scheduled',N'Enqueued');
select count(Id) from `{0}.received` where StatusName  in (N'Processing',N'Scheduled',N'Enqueued');", _prefix);

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
            var tableName = type == MessageType.Publish ? "published" : "received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, StatusName.Failed));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? "published" : "received";
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, tableName, StatusName.Succeeded));
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? "published" : "received";
            var where = string.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
                if (string.Equals(queryDto.StatusName, StatusName.Processing,
                    StringComparison.CurrentCultureIgnoreCase))
                    where += " and StatusName in (N'Processing',N'Scheduled',N'Enqueued')";
                else
                    where += " and StatusName=@StatusName";
            if (!string.IsNullOrEmpty(queryDto.Name))
                where += " and Name=@Name";
            if (!string.IsNullOrEmpty(queryDto.Group))
                where += " and Group=@Group";
            if (!string.IsNullOrEmpty(queryDto.Content))
                where += " and Content like '%@Content%'";

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
            return UseConnection(conn => GetNumberOfMessage(conn, "published", StatusName.Failed));
        }

        public int PublishedProcessingCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "published", StatusName.Processing));
        }

        public int PublishedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "published", StatusName.Succeeded));
        }

        public int ReceivedFailedCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", StatusName.Failed));
        }

        public int ReceivedProcessingCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", StatusName.Processing));
        }

        public int ReceivedSucceededCount()
        {
            return UseConnection(conn => GetNumberOfMessage(conn, "received", StatusName.Succeeded));
        }

        private int GetNumberOfMessage(IDbConnection connection, string tableName, string statusName)
        {
            var sqlQuery = statusName == StatusName.Processing
                ? $"select count(Id) from `{_prefix}.{tableName}` where StatusName in (N'Processing',N'Scheduled',N'Enqueued')"
                : $"select count(Id) from `{_prefix}.{tableName}` where StatusName = @state";

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
select aggr.* from (
    select date_format(`Added`,'%Y-%m-%d-%H') as `Key`,
        count(id) `Count`
    from  `{_prefix}.{tableName}`
    where StatusName = @statusName
    group by date_format(`Added`,'%Y-%m-%d-%H')
) aggr where `Key` in @keys;";

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