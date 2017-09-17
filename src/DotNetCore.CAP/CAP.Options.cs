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
        public const int DefaultPollingDelay = 15;

        /// <summary>
        /// Default processor count to process messages of cap.queue.
        /// </summary>
        public const int DefaultQueueProcessorCount = 2;

        /// <summary>
        /// Default succeeded message expriation timespan, in seconds.
        /// </summary>
        public const int DefaultSucceedMessageExpirationAfter = 24 * 3600;

        /// <summary>
        /// Failed message retry waiting interval.
        /// </summary>
        public const int DefaultFailedMessageWaitingInterval = 180;

        public CapOptions()
        {
            PollingDelay = DefaultPollingDelay;
            QueueProcessorCount = DefaultQueueProcessorCount;
            SucceedMessageExpiredAfter = DefaultSucceedMessageExpirationAfter;
            FailedMessageWaitingInterval = DefaultFailedMessageWaitingInterval;
            Extensions = new List<ICapOptionsExtension>();
        }

        /// <summary>
        /// Productor job polling delay time. 
        /// Default is 15 sec.
        /// </summary>
        public int PollingDelay { get; set; }

        /// <summary>
        /// Gets or sets the messages queue (Cap.Queue table) processor count.
        /// Default is 2 processor.
        /// </summary>
        public int QueueProcessorCount { get; set; }

        /// <summary>
        /// Sent or received succeed message after timespan of due, then the message will be deleted at due time. 
        /// Dafault is 24*3600 seconds.
        /// </summary>
        public int SucceedMessageExpiredAfter { get; set; }

        /// <summary>
        /// Failed messages polling delay time.
        /// Default is 180 seconds.
        /// </summary>
        public int FailedMessageWaitingInterval { get; set; }

        /// <summary>
        /// We’ll invoke this call-back with message type,name,content when requeue failed message.
        /// </summary>
        public Action<Models.MessageType, string, string> FailedCallback { get; set; }

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