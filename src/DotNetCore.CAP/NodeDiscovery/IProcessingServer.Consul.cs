using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    class ConsulProcessingNodeServer : IProcessingServer
    {
        private readonly DashboardOptions dashboardOptions;
        private readonly IDiscoveryProviderFactory discoveryProviderFactory;

        public ConsulProcessingNodeServer(
            DashboardOptions dashboardOptions,
            IDiscoveryProviderFactory discoveryProviderFactory)
        {
            this.dashboardOptions = dashboardOptions;
            this.discoveryProviderFactory = discoveryProviderFactory;
        }

        public void Start()
        {
            if (dashboardOptions.Discovery != null)
            {
                var discoveryProvider = discoveryProviderFactory.Get(dashboardOptions.Discovery);
                discoveryProvider.RegisterNode("192.168.2.55", dashboardOptions.Discovery.CurrentPort);
            }
        }

        public void Pulse()
        {

        }

        public void Dispose()
        {

        }
    }
}
