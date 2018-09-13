// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    internal class ConsumerHandler : IConsumerHandler
    {
        private readonly IStorageConnection _connection;
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly CancellationTokenSource _cts;
        private readonly IDispatcher _dispatcher;
        private readonly ILogger _logger;
        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);
        private readonly MethodMatcherCache _selector;

        private string _serverAddress;
        private Task _compositeTask;
        private bool _disposed;

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        public ConsumerHandler(IConsumerClientFactory consumerClientFactory,
            IDispatcher dispatcher,
            IStorageConnection connection,
            ILogger<ConsumerHandler> logger,
            MethodMatcherCache selector)
        {
            _selector = selector;
            _logger = logger;
            _consumerClientFactory = consumerClientFactory;
            _dispatcher = dispatcher;
            _connection = connection;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();

            foreach (var matchGroup in groupingMatches)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
                    {
                        _serverAddress = client.ServersAddress;

                        RegisterMessageProcessor(client);

                        client.Subscribe(matchGroup.Value.Select(x => x.Attribute.Name));

                        client.Listening(_pollingDelay, _cts.Token);
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            _compositeTask = Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts.Cancel();
            try
            {
                _compositeTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                {
                    _logger.ExpectedOperationCanceledException(innerEx);
                }
            }
        }

        public void Pulse()
        {
            //ignore
        }

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            client.OnMessageReceived += (sender, messageContext) =>
            {
                var startTime = DateTimeOffset.UtcNow;
                var stopwatch = Stopwatch.StartNew();

                var tracingResult = TracingBefore(messageContext.Name, messageContext.Content);
                var operationId = tracingResult.Item1;
                var messageBody = tracingResult.Item2;

                var receivedMessage = new CapReceivedMessage(messageContext)
                {
                    Id = SnowflakeId.Default().NextId(),
                    StatusName = StatusName.Scheduled,
                    Content = messageBody
                };

                try
                {
                    StoreMessage(receivedMessage);

                    client.Commit();

                    TracingAfter(operationId, receivedMessage.Name, receivedMessage.Content, startTime,
                        stopwatch.Elapsed);

                    _dispatcher.EnqueueToExecute(receivedMessage);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception occurred when storage received message. Message:'{0}'.", messageContext);

                    client.Reject();

                    TracingError(operationId, receivedMessage.Name, receivedMessage.Content, e, startTime,
                        stopwatch.Elapsed);
                }
            };

            client.OnLog += WriteLog;
        }

        private void WriteLog(object sender, LogMessageEventArgs logmsg)
        {
            switch (logmsg.LogType)
            {
                case MqLogType.ConsumerCancelled:
                    _logger.LogWarning("RabbitMQ consumer cancelled. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumerRegistered:
                    _logger.LogInformation("RabbitMQ consumer registered. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumerUnregistered:
                    _logger.LogWarning("RabbitMQ consumer unregistered. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumerShutdown:
                    _logger.LogWarning("RabbitMQ consumer shutdown. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumeError:
                    _logger.LogError("Kakfa client consume error. --> " + logmsg.Reason);
                    break;
                case MqLogType.ServerConnError:
                    _logger.LogCritical("Kafka server connection error. --> " + logmsg.Reason);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StoreMessage(CapReceivedMessage receivedMessage)
        {
             _connection.StoreReceivedMessage(receivedMessage);
        }

        private (Guid, string) TracingBefore(string topic, string values)
        {
            _logger.LogDebug("CAP received topic message:" + topic);

            Guid operationId = Guid.NewGuid();

            var eventData = new BrokerConsumeEventData(
                operationId, "",
                _serverAddress,
                topic,
                values,
                DateTimeOffset.UtcNow);

            s_diagnosticListener.WriteConsumeBefore(eventData);

            return (operationId, eventData.BrokerTopicBody);
        }

        private void TracingAfter(Guid operationId, string topic, string values, DateTimeOffset startTime, TimeSpan du)
        {
            var eventData = new BrokerConsumeEndEventData(
                operationId,
                "",
                _serverAddress,
                topic,
                values,
                startTime,
                du);

            s_diagnosticListener.WriteConsumeAfter(eventData);
        }

        private void TracingError(Guid operationId, string topic, string values, Exception ex, DateTimeOffset startTime, TimeSpan du)
        {
            var eventData = new BrokerConsumeErrorEventData(
                operationId,
                "",
                _serverAddress,
                topic,
                values,
                ex,
                startTime,
                du);

            s_diagnosticListener.WriteConsumeError(eventData);
        }
    }
}