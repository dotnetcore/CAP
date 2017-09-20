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
                    var configOptions = RequestServices.GetService<DiscoveryOptions>();

                    var factory = RequestServices.GetService<IDiscoveryProviderFactory>();

                    var discoryProvider = factory.Create(configOptions);

                    _nodes = discoryProvider.GetNodes().GetAwaiter().GetResult();
                }
                return _nodes;
            }
        }
    }
}
