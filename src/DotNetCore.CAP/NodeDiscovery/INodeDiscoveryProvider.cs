using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetCore.CAP.NodeDiscovery
{
    public interface INodeDiscoveryProvider
    {
        Task<IList<Node>> GetNodes();

        Task RegisterNode();
    }
}