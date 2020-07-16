// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Pulsar.Client.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Kafka
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly PulsarClient _client;
        private readonly PulsarOptions _options;
        private readonly ConcurrentQueue<IProducer<byte[]>> _producerPool;
        private int _pCount;
        private int _maxSize;

        public ConnectionPool(ILogger<ConnectionPool> logger, IOptions<PulsarOptions> options)
        {
            _options = options.Value;
            _client = new PulsarClientBuilder().ServiceUrl(_options.Servers).Build();
            _producerPool = new ConcurrentQueue<IProducer<byte[]>>();
            _maxSize = _options.ConnectionPoolSize;

            logger.LogDebug("CAP Pulsar configuration: {0}", JsonConvert.SerializeObject(_options.AsPulsarConfig(), Formatting.Indented));
        }

        public string ServersAddress => _options.Servers;

        public IProducer<byte[]> RentProducer()
        {
            if (_producerPool.TryDequeue(out var producer))
            {
                Interlocked.Decrement(ref _pCount);

                return producer;
            }

            producer = _client.NewProducer().Topic($"persistent://public/default/supermatelsotoppic").CreateAsync().Result;

            return producer;
        }

        public bool Return(IProducer<byte[]> producer)
        {
            if (Interlocked.Increment(ref _pCount) <= _maxSize)
            {
                _producerPool.Enqueue(producer);

                return true;
            }

            Interlocked.Decrement(ref _pCount);

            return false;
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_producerPool.TryDequeue(out var context))
            {
                context.DisposeAsync();

            }
        }
    }
}