using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class ConnectionChannelPool : IConnectionChannelPool, IDisposable
    {
        private const int DefaultPoolSize = 15;
        private readonly Func<IConnection> _connectionActivator;
        private readonly ILogger<ConnectionChannelPool> _logger;
        private readonly ConcurrentQueue<IModel> _pool = new ConcurrentQueue<IModel>();
        private IConnection _connection;

        private int _count;
        private int _maxSize;

        public ConnectionChannelPool(ILogger<ConnectionChannelPool> logger,
            RabbitMQOptions options)
        {
            _logger = logger;
            _maxSize = DefaultPoolSize;

            _connectionActivator = CreateConnection(options);
        }

        IModel IConnectionChannelPool.Rent()
        {
            return Rent();
        }

        bool IConnectionChannelPool.Return(IModel connection)
        {
            return Return(connection);
        }

        public IConnection GetConnection()
        {
            if (_connection != null && _connection.IsOpen)
                return _connection;
            _connection = _connectionActivator();
            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
            return _connection;
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
                context.Dispose();
        }

        private static Func<IConnection> CreateConnection(RabbitMQOptions options)
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

        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogWarning($"RabbitMQ client connection closed! {e}");
        }

        public virtual IModel Rent()
        {
            if (_pool.TryDequeue(out var model))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);

                return model;
            }

            model = GetConnection().CreateModel();

            return model;
        }

        public virtual bool Return(IModel connection)
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