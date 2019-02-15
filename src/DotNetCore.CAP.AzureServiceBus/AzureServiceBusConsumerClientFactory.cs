// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.AzureServiceBus
{
    internal sealed class AzureServiceBusConsumerClientFactory : IConsumerClientFactory
    {
        private readonly AzureServiceBusOptions _asbOptions;
        private readonly IConnectionPool _connectionPool;

        public AzureServiceBusConsumerClientFactory(
            AzureServiceBusOptions asbOptions,
            IConnectionPool connectionPool)
        {
            _asbOptions = asbOptions;
            _connectionPool = connectionPool;
        }

        public IConsumerClient Create(string groupId)
        {
            return new AzureServiceBusConsumerClient(groupId, _connectionPool, _asbOptions);
        }
    }
}