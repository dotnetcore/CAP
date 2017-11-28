using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class CapOptions
    {
        /// <summary>
        /// Default value for polling delay timeout, in seconds.
        /// </summary>
        public const int DefaultPollingDelay = 15;

        /// <summary>
        /// Default processor count to process messages of cap.queue.
        /// </summary>
        public const int DefaultQueueProcessorCount = 2;

        /// <summary>
        /// Default succeeded message expiration time span, in seconds.
        /// </summary>
        public const int DefaultSucceedMessageExpirationAfter = 24 * 3600;

        /// <summary>
        /// Failed message retry waiting interval.
        /// </summary>
        public const int DefaultFailedMessageWaitingInterval = 600;

        /// <summary>
        /// Failed message retry count.
        /// </summary>
        public const int DefaultFailedRetryCount = 100;


        public CapOptions()
        {
            PollingDelay = DefaultPollingDelay;
            QueueProcessorCount = DefaultQueueProcessorCount;
            SucceedMessageExpiredAfter = DefaultSucceedMessageExpirationAfter;
            FailedRetryInterval = DefaultFailedMessageWaitingInterval;
            FailedRetryCount = DefaultFailedRetryCount;
            Extensions = new List<ICapOptionsExtension>();
            DefaultGroup = "cap.queue." + Assembly.GetEntryAssembly().GetName().Name.ToLower();
        }

        internal IList<ICapOptionsExtension> Extensions { get; }

        /// <summary>
        /// Subscriber default group name. kafka-->group name. rabbitmq --> queue name.
        /// </summary>
        public string DefaultGroup { get; set; }

        /// <summary>
        /// Producer job polling delay time.
        /// Default is 15 sec.
        /// </summary>
        public int PollingDelay { get; set; }

        /// <summary>
        /// Gets or sets the messages queue (Cap.Queue table) processor count.
        /// Default is 2 processor.
        /// </summary>
        public int QueueProcessorCount { get; set; }

        /// <summary>
        /// Sent or received succeed message after time span of due, then the message will be deleted at due time.
        /// Default is 24*3600 seconds.
        /// </summary>
        public int SucceedMessageExpiredAfter { get; set; }

        /// <summary>
        /// Failed messages polling delay time.
        /// Default is 600 seconds.
        /// </summary>
        public int FailedRetryInterval { get; set; }

        /// <summary>
        /// We’ll invoke this call-back with message type,name,content when requeue failed message.
        /// </summary>
        public Action<MessageType, string, string> FailedCallback { get; set; }

        /// <summary>
        /// The number of message retries, the retry will stop when the threshold is reached.
        /// Default is 100 times.
        /// </summary>
        public int FailedRetryCount { get; set; }

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