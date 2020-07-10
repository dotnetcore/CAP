// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Amazon;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseAmazonSQS(this CapOptions options, RegionEndpoint region)
        {
            return options.UseAmazonSQS(opt => { opt.Region =  region; });
        }

        public static CapOptions UseAmazonSQS(this CapOptions options, Action<AmazonSQSOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new AmazonSQSOptionsExtension(configure));

            return options;
        }
    }
}