// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    internal class DiscoveryProviderFactory : IDiscoveryProviderFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public DiscoveryProviderFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public INodeDiscoveryProvider Create(DiscoveryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new ConsulNodeDiscoveryProvider(_loggerFactory, options);
        }
    }
}