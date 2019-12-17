// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;

namespace DotNetCore.CAP.Kafka
{
    public interface IConnectionPool
    {
        string ServersAddress { get; }

        IProducer<string, byte[]> RentProducer();

        bool Return(IProducer<string, byte[]> producer);
    }
}