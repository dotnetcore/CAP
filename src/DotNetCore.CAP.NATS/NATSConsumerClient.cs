// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using NATS.Client;

namespace DotNetCore.CAP.NATS
{
    internal sealed class NATSConsumerClient : IConsumerClient
    {
        private static readonly SemaphoreSlim ConnectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly string _groupId;
        private readonly NATSOptions _natsOptions;
        private readonly IList<IAsyncSubscription> _asyncSubscriptions;

        private IConnection _consumerClient;

        public NATSConsumerClient(string groupId, IOptions<NATSOptions> options)
        {
            _groupId = groupId;
            _asyncSubscriptions = new List<IAsyncSubscription>();
            _natsOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("NATS", _natsOptions.Servers);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            foreach (var topic in topics)
            {
                _asyncSubscriptions.Add(_consumerClient.SubscribeAsync(topic, _groupId));
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            foreach (var subscription in _asyncSubscriptions)
            {
                subscription.MessageHandler += Subscription_MessageHandler;
                subscription.Start();
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void Subscription_MessageHandler(object sender, MsgHandlerEventArgs e)
        {
            using var mStream = new MemoryStream();
            var binFormatter = new BinaryFormatter();

            mStream.Write(e.Message.Data, 0, e.Message.Data.Length);
            mStream.Position = 0;

            var message = (TransportMessage)binFormatter.Deserialize(mStream);
            message.Headers.Add(Headers.Group, _groupId);
            OnMessageReceived?.Invoke(e.Message.Reply, message);
        }

        public void Commit(object sender)
        {
            if (sender is string reply)
            {
                _consumerClient.Publish(reply, new byte[] { 1 });
            }
        }

        public void Reject(object sender)
        {
            if (sender is string reply)
            {
                _consumerClient.Publish(reply, new byte[] { 0 });
            }
        }

        public void Dispose()
        {
            _consumerClient?.Dispose();
        }

        public void Connect()
        {
            if (_consumerClient != null)
            {
                return;
            }

            ConnectionLock.Wait();

            try
            {
                if (_consumerClient == null)
                {
                    var opts = _natsOptions.Options ?? ConnectionFactory.GetDefaultOptions();
                    opts.Url = _natsOptions.Servers ?? opts.Url;
                    opts.ClosedEventHandler = ConnectedEventHandler;
                    opts.DisconnectedEventHandler = ConnectedEventHandler;
                    opts.AsyncErrorEventHandler = AsyncErrorEventHandler;
                    _consumerClient = new ConnectionFactory().CreateConnection(opts);
                }
            }
            finally
            {
                ConnectionLock.Release();
            }
        }

        private void ConnectedEventHandler(object sender, ConnEventArgs e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = $"An error occurred during connect NATS --> {e.Error}"
            };
            OnLog?.Invoke(null, logArgs);
        }

        private void AsyncErrorEventHandler(object sender, ErrEventArgs e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.AsyncErrorEvent,
                Reason = $"An error occurred out of band --> {e.Error}"
            };
            OnLog?.Invoke(null, logArgs);
        }
    }
}