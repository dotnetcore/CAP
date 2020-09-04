// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetMQ.Sockets;

namespace DotNetCore.CAP.ZeroMQ
{
    public class ConnectionChannelPool : IConnectionChannelPool, IDisposable
    {
        private const int DefaultPoolSize = 15;
        private readonly ILogger<ConnectionChannelPool> _logger;
        private readonly ConcurrentQueue<PublisherSocket> _pool;
        private static readonly object SLock = new object();

        private int _count;
        private int _maxSize;

        public ConnectionChannelPool(
            ILogger<ConnectionChannelPool> logger,
            IOptions<CapOptions> capOptionsAccessor,
            IOptions<ZeroMQOptions> optionsAccessor)
        {
            _logger = logger;
            _maxSize = DefaultPoolSize;
            _pool = new ConcurrentQueue<PublisherSocket>();

            var capOptions = capOptionsAccessor.Value;
            var options = optionsAccessor.Value;

             

            HostAddress = $"tcp://{options.HostName}:{options.PubPort}";
            Exchange = "v1" == capOptions.Version ? options.ExchangeName : $"{options.ExchangeName}.{capOptions.Version}";

            _logger.LogDebug($"ZeroMQ configuration:'HostName:{options.HostName}, PubPort:{options.PubPort}, UserName:{options.UserName}, Password:{options.Password}, ExchangeName:{options.ExchangeName}'");
        }

        PublisherSocket IConnectionChannelPool.Rent()
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

        bool IConnectionChannelPool.Return(PublisherSocket connection)
        {
            return Return(connection);
        }

        public string HostAddress { get; }

        public string Exchange { get; }

      

        public    void Dispose()
        {
            _maxSize = 0;

            while (_pool.TryDequeue(out var context))
            {
                context.Dispose();
            }
        }

        

    
        public virtual PublisherSocket Rent()
        {
            if (_pool.TryDequeue(out var model))
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0);

                return model;
            }

            try
            {
                model = new PublisherSocket();
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

        public virtual bool Return(PublisherSocket connection)
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