// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;

namespace DotNetCore.CAP.Kafka
{
    public interface IConnectionPool
    {
        string ServersAddress { get; }

        IProducer<Null,string> RentProducer();

        bool Return(IProducer<Null, string> producer);
    }
}