using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    interface IDiscoveryProviderFactory
    {
        INodeDiscoveryProvider Get(NodeConfiguration configuration);
    }
}
