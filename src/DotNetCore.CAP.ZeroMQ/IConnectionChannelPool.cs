// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NetMQ;

namespace DotNetCore.CAP.ZeroMQ
{
    public interface IConnectionChannelPool
    {
        string HostAddress { get; }

        string Exchange { get; }

        NetMQSocket Rent();

        bool Return(NetMQSocket context);
    }
}