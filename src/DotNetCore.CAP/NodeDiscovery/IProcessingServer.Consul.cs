using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    class ConsulProcessingNodeServer : IProcessingServer
    {
        private readonly DiscoveryOptions dashboardOptions;
        private readonly IDiscoveryProviderFactory discoveryProviderFactory;

        public ConsulProcessingNodeServer(
            DiscoveryOptions dashboardOptions,
            IDiscoveryProviderFactory discoveryProviderFactory)
        {
            this.dashboardOptions = dashboardOptions;
            this.discoveryProviderFactory = discoveryProviderFactory;
        }

        public void Start()
        {
            var discoveryProvider = discoveryProviderFactory.Create(dashboardOptions);

            discoveryProvider.RegisterNode();
        }

        public void Pulse()
        {
            //ignore
        }

        public void Dispose()
        {

        }
    }
}
