// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    internal class ConsulProcessingNodeServer : IProcessingServer
    {
        private readonly INodeDiscoveryProvider _discoveryProvider;

        public ConsulProcessingNodeServer(INodeDiscoveryProvider discoveryProvider)
        {
            _discoveryProvider = discoveryProvider;
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            await _discoveryProvider.RegisterNode(stoppingToken);
        }

        public void Dispose()
        {
        }
    }
}