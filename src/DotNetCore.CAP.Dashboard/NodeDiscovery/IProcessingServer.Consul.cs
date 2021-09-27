// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
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

        public void Start(CancellationToken stoppingToken)
        {
            _discoveryProvider.RegisterNode(stoppingToken).GetAwaiter().GetResult();
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