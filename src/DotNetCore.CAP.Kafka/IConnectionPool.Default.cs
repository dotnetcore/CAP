// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Kafka
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly KafkaOptions _options;
        private readonly ConcurrentQueue<IProducer<string, byte[]>> _producerPool;
        private int _pCount;
        private int _maxSize;

        public ConnectionPool(ILogger<ConnectionPool> logger, IOptions<KafkaOptions> options)
        {
            _options = options.Value;
            _producerPool = new ConcurrentQueue<IProducer<string, byte[]>>();
            _maxSize = _options.ConnectionPoolSize;

            logger.LogDebug("CAP Kafka configuration: {0}", JsonConvert.SerializeObject(_options.AsKafkaConfig(), Formatting.Indented));
        }

        public string ServersAddress => _options.Servers;

        public IProducer<string, byte[]> RentProducer()
        {
            if (_producerPool.TryDequeue(out var producer))
            {
                Interlocked.Decrement(ref _pCount);

                return producer;
            }

            producer = new ProducerBuilder<string, byte[]>(_options.AsKafkaConfig()).Build();

            return producer;
        }

        public bool Return(IProducer<string, byte[]> producer)
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
                context.Dispose();

            }
        }
    }
}