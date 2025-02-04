// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ;

public interface IConnectionChannelPool
{
    string HostAddress { get; }

    string Exchange { get; }

    IConnection GetConnection();

    Task<IChannel> Rent();

    bool Return(IChannel context);
}