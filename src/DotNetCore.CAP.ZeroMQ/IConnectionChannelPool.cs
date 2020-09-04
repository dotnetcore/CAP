// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NetMQ;
using NetMQ.Sockets;

namespace DotNetCore.CAP.ZeroMQ
{
    public interface IConnectionChannelPool
    {
        string HostAddress { get; }

        string Exchange { get; }


        PublisherSocket Rent();

        bool Return(PublisherSocket context);
    }
}