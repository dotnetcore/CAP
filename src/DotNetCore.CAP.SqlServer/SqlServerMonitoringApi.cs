using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;

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

        public JobList<DeletedJobDto> DeletedJobs(int from, int count)
        {
            return new JobList<DeletedJobDto>(new Dictionary<string, DeletedJobDto>());
        }

        public long DeletedListCount()
        {
            return 11;
        }

        public long EnqueuedCount(string queue)
        {
            return 11;
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        {
            return new JobList<EnqueuedJobDto>(new Dictionary<string, EnqueuedJobDto>());
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public long FailedCount()
        {
            return 11;
        }

        public JobList<FailedJobDto> FailedJobs(int from, int count)
        {
            return new JobList<FailedJobDto>(new Dictionary<string, FailedJobDto>());
        }

        public long FetchedCount(string queue)
        {
            return 11;
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage)
        {
            return new JobList<FetchedJobDto>(new Dictionary<string, FetchedJobDto>());
        }

        public StatisticsDto GetStatistics()
        {
            string sql = String.Format(@"
set transaction isolation level read committed;
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Enqueued';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Failed';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Processing';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Scheduled';
select 11;
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Successed';
select count(Id) from [{0}].Published with (nolock) where StatusName = N'Deleted';
select 14;
                ", _options.Schema);

            var statistics = UseConnection(connection =>
            {
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(sql))
                {
                    stats.Enqueued = multi.ReadSingle<int>();
                    stats.Failed = multi.ReadSingle<int>();
                    stats.Processing = multi.ReadSingle<int>();
                    stats.Scheduled = multi.ReadSingle<int>();

                    stats.Servers = multi.ReadSingle<int>();

                    stats.Succeeded = multi.ReadSingleOrDefault<long?>() ?? 0;
                    stats.Deleted = multi.ReadSingleOrDefault<long?>() ?? 0;

                    stats.Recurring = multi.ReadSingle<int>();
                }
                return stats;
            });

            //statistics.Queues = _storage.QueueProviders
            //    .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
            //    .Count();
            statistics.Queues = 3;
            return statistics;
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return UseConnection(connection =>
                GetHourlyTimelineStats(connection, "failed"));
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return UseConnection(connection =>
                 GetHourlyTimelineStats(connection, "succeeded"));
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return new JobDetailsDto();
        }

        public long ProcessingCount()
        {
            return 11;
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int from, int count)
        {
            return new JobList<ProcessingJobDto>(new Dictionary<string, ProcessingJobDto>());
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            return new List<QueueWithTopEnqueuedJobsDto>();
        }

        public long ScheduledCount()
        {
            return 0;
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int from, int count)
        {
            return new JobList<ScheduledJobDto>(new Dictionary<string, ScheduledJobDto>());
        }

        public IList<ServerDto> Servers()
        {
            return new List<ServerDto>();
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public JobList<SucceededJobDto> SucceededJobs(int from, int count)
        {
            return new JobList<SucceededJobDto>(new Dictionary<string, SucceededJobDto>());
        }

        public long SucceededListCount()
        {
            return 11;
        }

        private T UseConnection<T>(Func<IDbConnection, T> action)
        {
            return _storage.UseConnection(action);
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(IDbConnection connection, string type)
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

        private Dictionary<DateTime, long> GetTimelineStats(IDbConnection connection, string type)
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

        private Dictionary<DateTime, long> GetTimelineStats(
           IDbConnection connection,
           IDictionary<string, DateTime> keyMaps)
        {
            string sqlQuery =
$@"select [Key], [Value] as [Count] from [{_options.Schema}].AggregatedCounter with (nolock)
where [Key] in @keys";

            var valuesMap = connection.Query(
                sqlQuery,
                new { keys = keyMaps.Keys })
                .ToDictionary(x => (string)x.Key, x => (long)x.Count);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }
    }
}
