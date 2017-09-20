using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    interface IDiscoveryProviderFactory
    {
        INodeDiscoveryProvider Create(DiscoveryOptions options);
    }
}
