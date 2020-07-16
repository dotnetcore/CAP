// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using Microsoft.FSharp.Core;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Pulsar
{
    internal sealed class PulsarConsumerClient : IConsumerClient
    {
        private static readonly SemaphoreSlim ConnectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private static PulsarClient _client;
        private readonly string _groupId;
        private readonly PulsarOptions _pulsarOptions;
        private IConsumer<byte[]> _consumerClient;

        public PulsarConsumerClient(string groupId, IOptions<PulsarOptions> options)
        {
            _groupId = groupId;
            _pulsarOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
            
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("Pulsar", _pulsarOptions.Servers);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            _consumerClient = _client.NewConsumer()
                .Topic(topics.First())
                .SubscriptionName("test")
                .ConsumerName("testconsumer")
                .SubscriptionType(SubscriptionType.Shared)
                .SubscribeAsync().Result;
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            var task = ProcessMessage(_consumerClient, (message) =>
            {
                //var messageText = Encoding.UTF8.GetString(message.Data);
                return Task.CompletedTask;
            }, cancellationToken);
            task.RunSynchronously();
            // ReSharper disable once FunctionNeverReturns
        }

        internal async Task ProcessMessage(IConsumer<byte[]> consumer,
            Func<Message<byte[]>, Task> f, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    bool success = false;
                    Message<byte[]> consumerResult = consumer.ReceiveAsync().Result;
                    try
                    {
                        await f(consumerResult);
                        success = true;
                        Dictionary<string, string> headers = new Dictionary<string, string>();
                        headers.Add(Messages.Headers.MessageId, consumerResult.MessageId.EntryId.ToString());
                        headers.Add(Messages.Headers.MessageName, consumer.Topic);
                        headers.Add(Messages.Headers.Group, _groupId);
                        TransportMessage result = new TransportMessage(headers, consumerResult.Data);
                        OnMessageReceived?.Invoke(consumerResult, result); 
                    }
                    catch (Exception e)
                    {
                        Reject(consumerResult);
                        //logger.LogError(e, "Can't process message {0}, MessageId={1}", consumer.Topic, message.MessageId);
                    }

                    if (success)
                    {
                        Commit(consumerResult);
                    }
                }
            }
            catch (Exception ex)
            {
                //logger.LogError(ex, "ProcessMessages failed for {0}", consumer.Topic);
            }
            // ReSharper disable once FunctionNeverReturns 
        }

        public void Commit(object sender)
        {
            try
            {
                Message<byte[]> mSender = (Message<byte[]>) sender;
                _consumerClient.AcknowledgeAsync(mSender.MessageId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Reject(object sender)
        {
            try
            {
                Message<byte[]> mSender = (Message<byte[]>) sender;
                _consumerClient.NegativeAcknowledge(mSender.MessageId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            //_consumerClient.NegativeAcknowledge(sender);//Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient?.DisposeAsync();
        }

        public void Connect()
        {
            if (_client != null)
            {
                return;
            }

            ConnectionLock.Wait();

            try
            {
                if (_client == null)
                {
                    _pulsarOptions.MainConfig["group.id"] = _groupId;
                    _pulsarOptions.MainConfig["auto.offset.reset"] = "earliest";
                    var config = _pulsarOptions.AsPulsarConfig();
                    _client = new PulsarClientBuilder().ServiceUrl(_pulsarOptions.MainConfig["bootstrap.servers"]).Build();
                }
            }
            finally
            {
                ConnectionLock.Release();
            }
        }

        private void ConsumerClient_OnConsumeError(IConsumer<byte[]> consumer, Exception e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = $"An error occurred during connect pulsar --> {e.Message}"
            };
            OnLog?.Invoke(null, logArgs);
        }
    }
}