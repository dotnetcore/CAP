using System;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.NodeDiscovery
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
                throw new ArgumentNullException(nameof(options));

            return new ConsulNodeDiscoveryProvider(_loggerFactory, options);
        }
    }
}