// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetCore.CAP.ZeroMQ
{
    internal sealed class ZeroMQConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly ZeroMQOptions _ZeroMQOptions;
        private NetMQSocket _sub;

        public ZeroMQConsumerClient(string queueName,
            IConnectionChannelPool connectionChannelPool,
            IOptions<ZeroMQOptions> options)
        {
            _queueName = queueName;
            _connectionChannelPool = connectionChannelPool;
            _ZeroMQOptions = options.Value;
            _exchangeName = connectionChannelPool.Exchange;
            HostAddress = $"tcp://{_ZeroMQOptions.HostName}:{_ZeroMQOptions.SubPort}";
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("ZeroMQ", _ZeroMQOptions.HostName);

        public string HostAddress { get; private set; }

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }
            Connect();
            if (_ZeroMQOptions.Pattern == NetMQPattern.PubSub)
            {
                foreach (var topic in topics)
                {
                    ((SubscriberSocket)_sub).Subscribe(topic);
                }
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();
            while (true)
            {
                string topic = string.Empty;
                try
                {
                    var buffer = _sub.ReceiveMultipartMessage();
                    topic = buffer[0].ConvertToString();
                    string header = buffer[1].ConvertToString();
                    var body = buffer[2].ToByteArray();
                    var _header = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(header);
                    _header.Add(Messages.Headers.Group, _queueName);
                    var message = new TransportMessage(_header, body);
                    OnMessageReceived?.Invoke(_sub, message);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke(this, new LogMessageEventArgs() { LogType = MqLogType.ExceptionReceived, Reason = $"{_queueName}-{topic}-{ex.Message}" });
                }
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
        }

        public void Commit(object sender)
        {
        }

        public void Reject(object sender)
        {
        }

        public void Dispose()
        {
            _sub?.Dispose();
        }

        public void Connect()
        {
            if (_sub != null)
            {
                return;
            }
            _connectionLock.Wait();
            try
            {
                if (_sub == null)
                {
                    switch (_ZeroMQOptions.Pattern)
                    {
                        case NetMQPattern.PushPull:
                            _sub = new PullSocket();
                            break;

                        case NetMQPattern.PubSub:
                            _sub = new SubscriberSocket();
                            break;

                        default:
                            break;
                    }
                    _sub.Connect(HostAddress);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(this, new LogMessageEventArgs() { LogType = MqLogType.ExceptionReceived, Reason = $"{HostAddress }-{_queueName}-{ex.Message}" });
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}