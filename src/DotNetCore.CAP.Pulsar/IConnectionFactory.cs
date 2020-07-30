// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Pulsar.Client.Api;

namespace DotNetCore.CAP.Pulsar
{
    public interface IConnectionFactory
    {
        string ServersAddress { get; }

        Task<IProducer<byte[]>> CreateProducerAsync(string topic);

        PulsarClient RentClient();
    }
}