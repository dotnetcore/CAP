// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace DotNetCore.CAP.ZeroMQ
{
    public enum NetMQPattern
    {
        PushPull,
        PubSub
    }

    public class ConnectionChannelPool : IConnectionChannelPool, IDisposable
    {
        private const int DefaultPoolSize = 15;
        private readonly ILogger<ConnectionChannelPool> _logger;
        private readonly ConcurrentQueue<NetMQSocket> _pool;
        private static readonly object SLock = new object();
        private readonly ZeroMQOptions _options;
        private int _count;
        private int _maxSize;

        public ConnectionChannelPool(
            ILogger<ConnectionChannelPool> logger,
            IOptions<CapOptions> capOptionsAccessor,
            IOptions<ZeroMQOptions> optionsAccessor)
        {
            _logger = logger;
            _maxSize = DefaultPoolSize;
            _pool = new ConcurrentQueue<NetMQSocket>();

            var capOptions = capOptionsAccessor.Value;
            _options = optionsAccessor.Value;

            HostAddress = $"tcp://{_options.HostName}:{_options.PubPort}";
            Exchange = "v1" == capOptions.Version ? _options.ExchangeName : $"{_options.ExchangeName}.{capOptions.Version}";

            _logger.LogDebug($"ZeroMQ configuration:'HostName:{_options.HostName}, PubPort:{_options.PubPort}, UserName:{_options.UserName}, Password:{_options.Password}, ExchangeName:{_options.ExchangeName}'");
        }

        NetMQSocket IConnectionChannelPool.Rent()
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

        bool IConnectionChannelPool.Return(NetMQSocket connection)
        {
            return Return(connection);
        }

        public string HostAddress { get; }

        public string Exchange { get; }

        public void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
            {
                context.Dispose();
            }
        }

        public virtual NetMQSocket Rent()
        {
            if (_pool.TryDequeue(out var model))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);
                return model;
            }

            try
            {
                switch (_options.Pattern)
                {
                    case NetMQPattern.PushPull:
                        model = new PushSocket();
                        break;

                    case NetMQPattern.PubSub:
                        model = new PublisherSocket();
                        break;

                    default:
                        break;
                }
                model.Connect(HostAddress);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ZeroMQ channel model create failed!");
                Console.WriteLine(e);
                throw;
            }

            return model;
        }

        public virtual bool Return(NetMQSocket connection)
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