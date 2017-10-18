using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private const int DefaultPoolSize = 15;

        private readonly Func<IConnection> _activator;

        private readonly ConcurrentQueue<IConnection> _pool = new ConcurrentQueue<IConnection>();
        private int _count;

        private int _maxSize;

        public ConnectionPool(RabbitMQOptions options)
        {
            _maxSize = DefaultPoolSize;

            _activator = CreateActivator(options);
        }

        IConnection IConnectionPool.Rent()
        {
            return Rent();
        }

        bool IConnectionPool.Return(IConnection connection)
        {
            return Return(connection);
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
                context.Dispose();
        }

        private static Func<IConnection> CreateActivator(RabbitMQOptions options)
        {
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                UserName = options.UserName,
                Port = options.Port,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                RequestedConnectionTimeout = options.RequestedConnectionTimeout,
                SocketReadTimeout = options.SocketReadTimeout,
                SocketWriteTimeout = options.SocketWriteTimeout
            };

            return () => factory.CreateConnection();
        }

        public virtual IConnection Rent()
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

        public virtual bool Return(IConnection connection)
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