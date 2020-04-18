// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.CAP.Messages;

// ReSharper disable InconsistentNaming

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class CapOptions
    {
        public CapOptions()
        {
            SucceedMessageExpiredAfter = 24 * 3600;
            FailedRetryInterval = 60;
            FailedRetryCount = 50;
            ConsumerThreadCount = 1;
            Extensions = new List<ICapOptionsExtension>();
            Version = "v1";
            DefaultGroup = "cap.queue." + Assembly.GetEntryAssembly()?.GetName().Name.ToLower();
        }

        internal IList<ICapOptionsExtension> Extensions { get; }

        /// <summary>
        /// Subscriber default group name. kafka-->group name. rabbitmq --> queue name.
        /// </summary>
        public string DefaultGroup { get; set; }

        /// <summary>
        /// The default version of the message, configured to isolate data in the same instance. The length must not exceed 20
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Sent or received succeed message after time span of due, then the message will be deleted at due time.
        /// Default is 24*3600 seconds.
        /// </summary>
        public int SucceedMessageExpiredAfter { get; set; }

        /// <summary>
        /// Failed messages polling delay time.
        /// Default is 60 seconds.
        /// </summary>
        public int FailedRetryInterval { get; set; }

        /// <summary>
        /// We’ll invoke this call-back with message type,name,content when retry failed (send or executed) messages equals <see cref="FailedRetryCount"/> times.
        /// </summary>
        public Action<FailedInfo> FailedThresholdCallback { get; set; }

        /// <summary>
        /// The number of message retries, the retry will stop when the threshold is reached.
        /// Default is 50 times.
        /// </summary>
        public int FailedRetryCount { get; set; }

        /// <summary>
        /// The number of consumer thread connections.
        /// Default is 1
        /// </summary>
        public int ConsumerThreadCount { get; set; }

        /// <summary>
        /// Registers an extension that will be executed when building services.
        /// </summary>
        /// <param name="extension"></param>
        public void RegisterExtension(ICapOptionsExtension extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            Extensions.Add(extension);
        }
    }
}