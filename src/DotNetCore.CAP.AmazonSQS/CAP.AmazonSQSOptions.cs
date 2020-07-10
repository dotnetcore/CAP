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
        public RegionEndpoint Region { get; set; }

        public AWSCredentials Credentials { get; set; }
    }
}