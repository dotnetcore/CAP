// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP Google Cloud Platform Pub/Sub project.
    /// </summary>
    public class GcpPubSubOptions
    {
        /// <summary>
        /// The GCP <c>Project</c> ID.
        /// </summary>
        public string ProjectId { get; set; }

        public string TopicId { get; set; } = "CAP";

        public string VerificationToken { get; set; }

        public string SubscriptionId { get; set; }
    }
}