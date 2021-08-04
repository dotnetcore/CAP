// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client;

namespace DotNetCore.CAP.NATS
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly NATSOptions _options;
        private readonly ConcurrentQueue<IConnection> _connectionPool;
        private readonly ConnectionFactory _connectionFactory;
        private int _pCount;
        private int _maxSize;

        public ConnectionPool(ILogger<ConnectionPool> logger, IOptions<NATSOptions> options)
        {
            _options = options.Value;
            _connectionPool = new ConcurrentQueue<IConnection>();
            _connectionFactory = new ConnectionFactory();
            _maxSize = _options.ConnectionPoolSize;

            logger.LogDebug("NATS configuration: {0}", options.Value.Options);
        }

        public string ServersAddress => _options.Servers;

        public IConnection RentConnection()
        {
            if (_connectionPool.TryDequeue(out var connection))
            {
                Interlocked.Decrement(ref _pCount);

                return connection;
            }

            if (_options.Options != null)
            {
                if (_options.Servers != null)
                {
                    _options.Options.Url = _options.Servers;
                }
                connection = _connectionFactory.CreateConnection(_options.Options);
            }
            else
            {
                connection = _connectionFactory.CreateConnection(_options.Servers);
            }

            return connection;
        }

        public bool Return(IConnection connection)
        {
            if (Interlocked.Increment(ref _pCount) <= _maxSize)
            {
                _connectionPool.Enqueue(connection);

                return true;
            }

            connection.Dispose();

            Interlocked.Decrement(ref _pCount);

            return false;
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_connectionPool.TryDequeue(out var context))
            {
                context.Dispose();

            }
        }
    }
}