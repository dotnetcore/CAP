// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon;
using Amazon.Runtime;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    // ReSharper disable once InconsistentNaming
    public class AmazonSQSOptions
    {
        public RegionEndpoint Region { get; set; } = default!;

        public AWSCredentials? Credentials { get; set; }

        /// <summary>
        /// Overrides Service Url deduced from AWS Region. To use in local development environments like localstack.
        /// </summary>
        public string? SNSServiceUrl { get; set; }

        /// <summary>
        /// Overrides Service Url deduced from AWS Region. To use in local development environments like localstack.
        /// </summary>
        public string? SQSServiceUrl { get; set; }

    }
}