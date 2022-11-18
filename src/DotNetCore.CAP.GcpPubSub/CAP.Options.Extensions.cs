// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        /// <summary>
        /// Configuration to use Google Cloud Pub/Sub in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="projectId">The GCP <c>Project</c> ID.</param>
        public static CapOptions UseGooglePubSub(this CapOptions options,string projectId)
        {
            return options.UseGooglePubSub(opt =>
            {
                opt.ProjectId = projectId;
            });
        }

        /// <summary>
        /// Configuration to use Google Cloud Pub/Sub in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="configure">Provides programmatic configuration for the Google Cloud Pub/Sub.</param>
        public static CapOptions UseGooglePubSub(this CapOptions options, Action<GcpPubSubOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new GcpPubSubOptionsExtension(configure));

            return options;
        }
    }
}