// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotNetCore.CAP.AzureServiceBus
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly Func<ServiceBusConnection> _activator;
        private readonly ConcurrentQueue<ServiceBusConnection> _pool;
        private int _count;
        private int _maxSize;

        public ConnectionPool(ILogger<ConnectionPool> logger, AzureServiceBusOptions options)
        {
            _pool = new ConcurrentQueue<ServiceBusConnection>();
            _maxSize = options.ConnectionPoolSize;
            _activator = CreateActivator(options);

            ConnectionString = options.ConnectionString ?? options.ConnectionStringBuilder.ToString();

            logger.LogDebug("Azure Service Bus configuration of CAP :\r\n {0}",
                JsonConvert.SerializeObject(options, Formatting.Indented));
        }

        public string ConnectionString { get; }

        ServiceBusConnection IConnectionPool.Rent()
        {
            return Rent();
        }

        bool IConnectionPool.Return(ServiceBusConnection connection)
        {
            return Return(connection);
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
            {
                context.CloseAsync().Wait();
            }
        }

        private static Func<ServiceBusConnection> CreateActivator(AzureServiceBusOptions options)
        {
            if (options.ConnectionStringBuilder != null)
            {
                return () => new ServiceBusConnection(options.ConnectionStringBuilder);
            }

            return () => new ServiceBusConnection(options.ConnectionString, TimeSpan.FromSeconds(30), RetryPolicy.Default);
        }

        public virtual ServiceBusConnection Rent()
        {
            if (_pool.TryDequeue(out var connection))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);

                if (connection.IsClosedOrClosing)
                {
                    connection = _activator();
                }
                return connection;
            }

            connection = _activator();

            return connection;
        }

        public virtual bool Return(ServiceBusConnection connection)
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