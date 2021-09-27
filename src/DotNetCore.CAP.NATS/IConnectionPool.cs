// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NATS.Client;

namespace DotNetCore.CAP.NATS
{
    public interface IConnectionPool
    {
        string ServersAddress { get; }

        IConnection RentConnection();

        bool Return(IConnection connection);
    }
}