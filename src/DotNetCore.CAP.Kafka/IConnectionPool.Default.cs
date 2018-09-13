﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Kafka
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly ILogger<ConnectionPool> _logger;
        private readonly Func<Producer> _activator;
        private readonly ConcurrentQueue<Producer> _pool;
        private int _count;
        private int _maxSize;

        public ConnectionPool(ILogger<ConnectionPool> logger, KafkaOptions options)
        {
            _logger = logger;
            _pool = new ConcurrentQueue<Producer>();
            _maxSize = options.ConnectionPoolSize;
            _activator = CreateActivator(options);
            ServersAddress = options.Servers;

            _logger.LogDebug("Kafka configuration of CAP :\r\n {0}",
                JsonConvert.SerializeObject(options.AsKafkaConfig(), Formatting.Indented));
        }

        public string ServersAddress { get; }

        Producer IConnectionPool.Rent()
        {
            return Rent();
        }

        bool IConnectionPool.Return(Producer connection)
        {
            return Return(connection);
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
            {
                context.Dispose();
            }
        }

        private static Func<Producer> CreateActivator(KafkaOptions options)
        {
            return () => new Producer(options.AsKafkaConfig());
        }

        public virtual Producer Rent()
        {
            if (_pool.TryDequeue(out var connection))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);

                return connection;
            }

            connection = _activator();

            return connection;
        }

        public virtual bool Return(Producer connection)
        {
            if (Interlocked.Increment(ref _count) <= _maxSize)
            {
                _pool.Enqueue(connection);

                return true;
            }

            Interlocked.Decrement(ref _count);

            Debug.Assert(_maxSize == 0 || _pool.Count <= _maxSize);

            return false;
        }
    }
}