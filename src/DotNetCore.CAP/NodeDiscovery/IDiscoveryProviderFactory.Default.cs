using System;

namespace DotNetCore.CAP.NodeDiscovery
{
    internal class DiscoveryProviderFactory : IDiscoveryProviderFactory
    {
        public INodeDiscoveryProvider Create(DiscoveryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new ConsulNodeDiscoveryProvider(options);
        }
    }
}