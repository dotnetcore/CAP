using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.NodeDiscovery
{
    interface INodeDiscoveryProvider
    {
        Task<IList<Node>> GetNodes();

        Task RegisterNode();
    }
}
