using System;
using System.Collections.Generic;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class CapOptions
    {
        internal IList<ICapOptionsExtension> Extensions { get; private set; }

        /// <summary>
        /// Default value for polling delay timeout, in seconds.
        /// </summary>
        public const int DefaultPollingDelay = 8;

        /// <summary>
        /// Default value for CAP job.
        /// </summary>
        public const string DefaultCronExp = "* * * * *";

        public CapOptions()
        {
            CronExp = DefaultCronExp;
            PollingDelay = DefaultPollingDelay;
            Extensions = new List<ICapOptionsExtension>();
        }

        /// <summary>
        /// Corn expression for configuring retry cron job. Default is 1 min.
        /// </summary>
        public string CronExp { get; set; }

        /// <summary>
        /// Productor job polling delay time. Default is 8 sec.
        /// </summary>
        public int PollingDelay { get; set; } = 8;

        /// <summary>
		/// Registers an extension that will be executed when building services.
		/// </summary>
		/// <param name="extension"></param>
		public void RegisterExtension(ICapOptionsExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            Extensions.Add(extension);
        }
    }
}