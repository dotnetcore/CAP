using System;
using System.Collections.Generic;
using System.Text;

namespace Cap.Consistency.Job
{
    /// <summary>
    /// Represents a cron job to be executed at specified intervals of time.
    /// </summary>
    public class CronJob
    {
        public CronJob() {
            Id = Guid.NewGuid().ToString();
        }

        public CronJob(string cron)
            : this() {
            Cron = cron;
        }

        public CronJob(string cron, DateTime lastRun)
            : this(cron) {
            LastRun = lastRun;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string TypeName { get; set; }

        public string Cron { get; set; }

        public DateTime LastRun { get; set; }
    }
}
