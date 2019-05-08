// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Kafka
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly KafkaOptions _options;
        private readonly ConcurrentQueue<IProducer<Null, string>> _producerPool;
        private int _pCount;
        private int _maxSize;

        public ConnectionPool(ILogger<ConnectionPool> logger, KafkaOptions options)
        {
            ServersAddress = options.Servers;

            _options = options;
            _producerPool = new ConcurrentQueue<IProducer<Null, string>>();
            _maxSize = options.ConnectionPoolSize;

            logger.LogDebug("Kafka configuration of CAP :\r\n {0}", JsonConvert.SerializeObject(options.AsKafkaConfig(), Formatting.Indented));
        }

        public string ServersAddress { get; }

        public IProducer<Null, string> RentProducer()
        {
            if (_producerPool.TryDequeue(out var producer))
            {
                Interlocked.Decrement(ref _pCount);

                return producer;
            }

            producer = new ProducerBuilder<Null, string>(_options.AsKafkaConfig()).Build();

            return producer;
        }

        public bool Return(IProducer<Null, string> producer)
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