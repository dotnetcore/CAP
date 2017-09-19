using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    class DiscoveryProviderFactory : IDiscoveryProviderFactory
    {
        public INodeDiscoveryProvider Get(NodeConfiguration configuration)
        {
            if (configuration == null)
            {
                return null;
            }

            return new ConsulNodeDiscoveryProvider(configuration.ServerHostName, configuration.ServerProt);
        }
    }
}
