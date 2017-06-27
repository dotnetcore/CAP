using DotNetCore.CAP.Job;

namespace DotNetCore.CAP.Infrastructure
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class CapOptions
    {
         /// <summary>
        /// Corn expression for configuring retry cron job. Default is 1 min.
        /// </summary>
        public string CronExp { get; set; } = Cron.Minutely();

        /// <summary>
        /// Productor job polling delay time. Default is 8 sec.
        /// </summary>
        public int PollingDelay { get; set; } = 8;
    }
}