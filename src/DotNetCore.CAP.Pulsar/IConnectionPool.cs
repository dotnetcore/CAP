// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Pulsar.Client.Api;

namespace DotNetCore.CAP.Pulsar
{
    public interface IConnectionPool
    {
        string ServersAddress { get; }

        IProducer<byte[]> RentProducer();

        bool Return(IProducer<byte[]> producer);
    }
}