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

        public CapOptions()
        {
            PollingDelay = DefaultPollingDelay;
            Extensions = new List<ICapOptionsExtension>();
        }

        /// <summary>
        /// Productor job polling delay time. Default is 8 sec.
        /// </summary>
        public int PollingDelay { get; set; } = 8;

        /// <summary>
        /// We’ll send a POST request to the URL below with details of any subscribed events.
        /// </summary>
        public WebHook WebHook { get; set; }

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

    public class WebHook
    {
        public string PayloadUrl { get; set; }

        public string Secret { get; set; }
    }
}