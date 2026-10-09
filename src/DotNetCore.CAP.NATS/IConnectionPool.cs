// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using NATS.Client.Core;

namespace DotNetCore.CAP.NATS;

public interface IConnectionPool
{
    string ServersAddress { get; }

    ValueTask<INatsConnection> RentConnectionAsync();

    ValueTask<bool> ReturnAsync(INatsConnection connection);
}
