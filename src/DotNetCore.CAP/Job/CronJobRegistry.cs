using System;
using System.Collections.Generic;
using System.Reflection;
using NCrontab;

namespace DotNetCore.CAP.Job
{
    public abstract class CronJobRegistry
    {
        private List<Entry> _entries;

        public CronJobRegistry()
        {
            _entries = new List<Entry>();
        }

        protected void RegisterJob<T>(string name, string cron, RetryBehavior retryBehavior = null)
            where T : IJob
        {
            RegisterJob(name, typeof(T), cron, retryBehavior);
        }

        /// <summary>
        /// Registers a cron job.
        /// </summary>
        /// <param name="name">The name of the job.</param>
        /// <param name="jobType">The job's type.</param>
        /// <param name="cron">The cron expression to use.</param>
        /// <param name="retryBehavior">The <see cref="RetryBehavior"/> to use.</param>
        protected void RegisterJob(string name, Type jobType, string cron, RetryBehavior retryBehavior = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(cron));
            if (jobType == null) throw new ArgumentNullException(nameof(jobType));
            if (cron == null) throw new ArgumentNullException(nameof(cron));
            retryBehavior = retryBehavior ?? RetryBehavior.DefaultRetry;

            CrontabSchedule.TryParse(cron);

            if (!typeof(IJob).GetTypeInfo().IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    "Cron jobs should extend IJob.", nameof(jobType));
            }

            _entries.Add(new Entry(name, jobType, cron));
        }

        public Entry[] Build() => _entries.ToArray();

        public class Entry
        {
            public Entry(string name, Type jobType, string cron)
            {
                Name = name;
                JobType = jobType;
                Cron = cron;
            }

            public string Name { get; set; }

            public Type JobType { get; set; }

            public string Cron { get; set; }

            public RetryBehavior RetryBehavior { get; set; }
        }
    }
}