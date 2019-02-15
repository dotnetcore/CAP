// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus
{
    public interface IConnectionPool
    {
        string ConnectionString { get; }

        ServiceBusConnection Rent();

        bool Return(ServiceBusConnection context);
    }
}