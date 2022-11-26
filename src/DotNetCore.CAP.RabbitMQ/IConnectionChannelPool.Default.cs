// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class ConnectionChannelPool : IConnectionChannelPool, IDisposable
    {
        private const int DefaultPoolSize = 15;

        private readonly Func<IConnection> _connectionActivator;
        private readonly ILogger<ConnectionChannelPool> _logger;
        private readonly ConcurrentQueue<IModel> _pool;
        private readonly bool _isPublishConfirms;
        private IConnection? _connection;
        private static readonly object SLock = new();

        private int _count;
        private int _maxSize;

        public ConnectionChannelPool(
            ILogger<ConnectionChannelPool> logger,
            IOptions<CapOptions> capOptionsAccessor,
            IOptions<RabbitMQOptions> optionsAccessor)
        {
            _logger = logger;
            _maxSize = DefaultPoolSize;
            _pool = new ConcurrentQueue<IModel>();

            var capOptions = capOptionsAccessor.Value;
            var options = optionsAccessor.Value;

            _connectionActivator = CreateConnection(options);
            _isPublishConfirms = options.PublishConfirms;

            HostAddress = $"{options.HostName}:{options.Port}";
            Exchange = "v1" == capOptions.Version ? options.ExchangeName : $"{options.ExchangeName}.{capOptions.Version}";

            _logger.LogDebug($"RabbitMQ configuration:'HostName:{options.HostName}, Port:{options.Port}, UserName:{options.UserName}, Password:{options.Password}, ExchangeName:{options.ExchangeName}'");
        }

        IModel IConnectionChannelPool.Rent()
        {
            lock (SLock)
            {
                while (_count > _maxSize)
                {
                    Thread.SpinWait(1);
                }
                return Rent();
            }
        }

        bool IConnectionChannelPool.Return(IModel connection)
        {
            return Return(connection);
        }

        public string HostAddress { get; }

        public string Exchange { get; }

        public IConnection GetConnection()
        {
            lock (SLock)
            {
                if (_connection != null && _connection.IsOpen)
                {
                    return _connection;
                }

                _connection?.Dispose();
                _connection = _connectionActivator();
                return _connection;
            }
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var channel))
            {
                channel.Dispose();
            }
            _connection?.Dispose();
        }

        private static Func<IConnection> CreateConnection(RabbitMQOptions options)
        {
            var factory = new ConnectionFactory
            {
                UserName = options.UserName,
                Port = options.Port,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                DispatchConsumersAsync = true,
                ClientProvidedName = Assembly.GetEntryAssembly()?.GetName().Name!.ToLower()
            };

            if (options.HostName.Contains(","))
            {
                options.ConnectionFactoryOptions?.Invoke(factory);

                return () => factory.CreateConnection(AmqpTcpEndpoint.ParseMultiple(options.HostName));
            }
            factory.HostName = options.HostName;
            options.ConnectionFactoryOptions?.Invoke(factory);
            return () => factory.CreateConnection();
        }

        public virtual IModel Rent()
        {
            if (_pool.TryDequeue(out var model))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);

                return model;
            }

            try
            {
                model = GetConnection().CreateModel();
                model.ExchangeDeclare(Exchange, RabbitMQOptions.ExchangeType, true);
                if (_isPublishConfirms)
                {
                    model.ConfirmSelect();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RabbitMQ channel model create failed!");
                Console.WriteLine(e);
                throw;
            }

            return model;
        }

        public virtual bool Return(IModel channel)
        {
            if (Interlocked.Increment(ref _count) <= _maxSize && channel.IsOpen)
            {
                _pool.Enqueue(channel);

                return true;
            }

            channel.Dispose();

            Interlocked.Decrement(ref _count);

            Debug.Assert(_maxSize == 0 || _pool.Count <= _maxSize);

            return false;
        }
    }
}