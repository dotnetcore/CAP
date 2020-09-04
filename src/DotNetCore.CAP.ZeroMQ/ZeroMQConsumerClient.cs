// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;

using Headers = DotNetCore.CAP.Messages.Headers;

namespace DotNetCore.CAP.ZeroMQ
{
    internal sealed class ZeroMQConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly ZeroMQOptions _ZeroMQOptions;
    

        private SubscriberSocket _sub;

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

            foreach (var topic in topics)
            {
                _sub.Subscribe(topic);
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            while (true)
            {
                try
                {
                    //msg.Append(message.GetName());
                    //msg.Append(message.GetId());
                    //msg.Append(message.GetGroup() ?? "ZeroMQ");
                    //msg.Append(Newtonsoft.Json.JsonConvert.SerializeObject(message.Headers.ToDictionary(x => x.Key, x => (object)x.Value)));
                    //msg.Append(message.Body);

                    var buffer = _sub.ReceiveMultipartMessage();
                    string name = buffer[0].ConvertToString();
                    string id = buffer[1].ConvertToString();
                    string group = buffer[2].ConvertToString();
                    string header = buffer[3].ConvertToString();
                    var body = buffer[4].ToByteArray();
                    var _header = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(header);
                    var message = new TransportMessage(_header, body);
                    OnMessageReceived?.Invoke(_sub, message);
                }
                catch (Exception)
                {

                }
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }

            // ReSharper disable once FunctionNeverReturns
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

                    _sub = new SubscriberSocket();
                    _sub.Connect(HostAddress);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        #region events
 

       

        #endregion
    }
}