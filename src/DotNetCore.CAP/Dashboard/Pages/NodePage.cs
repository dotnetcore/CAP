using System;
using System.Collections.Generic;
using DotNetCore.CAP.NodeDiscovery;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard.Pages
{
    partial class NodePage
    {
        private IList<Node> _nodes = null;
        private INodeDiscoveryProvider _discoveryProvider;

        public NodePage()
        {

        }

        public NodePage(string id)
        {
            CurrentNodeId = id;
        }

        public string CurrentNodeId { get; set; }

        public IList<Node> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _discoveryProvider = RequestServices.GetService<INodeDiscoveryProvider>();
                    _nodes = _discoveryProvider.GetNodes().GetAwaiter().GetResult();                    
                }
                return _nodes;
            }
        }
    }
}
