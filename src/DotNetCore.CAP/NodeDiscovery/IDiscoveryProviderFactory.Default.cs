using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    class DiscoveryProviderFactory : IDiscoveryProviderFactory
    {
        public INodeDiscoveryProvider Create(DiscoveryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new ConsulNodeDiscoveryProvider(options);
        }
    }
}
