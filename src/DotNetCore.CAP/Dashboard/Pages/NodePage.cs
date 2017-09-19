using System;
using System.Collections.Generic;
using DotNetCore.CAP.NodeDiscovery;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard.Pages
{
    partial class NodePage
    {
        private IList<Node> _nodes = null;

        public IList<Node> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    var configOptions = RequestServices.GetService<DashboardOptions>();
                    var discoveryServer = configOptions.Discovery;
                    if (discoveryServer == null)
                        return null;

                    var factory = RequestServices.GetService<IDiscoveryProviderFactory>();
                    var discoryProvider = factory.Get(discoveryServer);
                    _nodes = discoryProvider.GetNodes().GetAwaiter().GetResult();
                }
                return _nodes;
            }
        }
    }
}
