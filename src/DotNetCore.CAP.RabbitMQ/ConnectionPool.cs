using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private const int DefaultPoolSize = 32;

        private readonly ConcurrentQueue<IConnection> _pool = new ConcurrentQueue<IConnection>();

        private readonly Func<IConnection> _activator;

        private int _maxSize;
        private int _count;

        public ConnectionPool(RabbitMQOptions options)
        {
            _maxSize = DefaultPoolSize;

            _activator = CreateActivator(options);
        }

        private static Func<IConnection> CreateActivator(RabbitMQOptions options)
        {
            var factory = new ConnectionFactory()
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
            if (_pool.TryDequeue(out IConnection connection))
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

        IConnection IConnectionPool.Rent() => Rent();

        bool IConnectionPool.Return(IConnection connection) => Return(connection);

        public void Dispose()
        {
            _maxSize = 0;

            IConnection context;
            while (_pool.TryDequeue(out context))
            {
                context.Dispose();
            }
        }
    }
}
