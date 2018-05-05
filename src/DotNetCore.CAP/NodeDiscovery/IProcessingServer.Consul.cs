// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.NodeDiscovery
{
    internal class ConsulProcessingNodeServer : IProcessingServer
    {
        private readonly DiscoveryOptions _dashboardOptions;
        private readonly IDiscoveryProviderFactory _discoveryProviderFactory;

        public ConsulProcessingNodeServer(
            DiscoveryOptions dashboardOptions,
            IDiscoveryProviderFactory discoveryProviderFactory)
        {
            _dashboardOptions = dashboardOptions;
            _discoveryProviderFactory = discoveryProviderFactory;
        }

        public void Start()
        {
            var discoveryProvider = _discoveryProviderFactory.Create(_dashboardOptions);

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