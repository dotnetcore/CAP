// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    internal class ConsumerRegister : IConsumerRegister
    {
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly IDispatcher _dispatcher;
        private readonly ISerializer _serializer;
        private readonly IDataStorage _storage;
        private readonly ILogger _logger;
        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);
        private readonly CapOptions _options;
        private readonly MethodMatcherCache _selector;
        private readonly CancellationTokenSource _cts;

        private string _serverAddress;
        private Task _compositeTask;
        private bool _disposed;
        private static bool _isHealthy = true;

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        public ConsumerRegister(
            IOptions<CapOptions> options,
            IConsumerClientFactory consumerClientFactory,
            IDispatcher dispatcher,
            ISerializer serializer,
            IDataStorage storage,
            ILogger<ConsumerRegister> logger,
            MethodMatcherCache selector)
        {
            _options = options.Value;
            _selector = selector;
            _logger = logger;
            _consumerClientFactory = consumerClientFactory;
            _dispatcher = dispatcher;
            _serializer = serializer;
            _storage = storage;
            _cts = new CancellationTokenSource();
        }

        public bool IsHealthy()
        {
            return _isHealthy;
        }

        public void Start()
        {
            var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();

            foreach (var matchGroup in groupingMatches)
            {
                for (int i = 0; i < _options.ConsumerThreadCount; i++)
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            using (var client = _consumerClientFactory.Create(matchGroup.Key))
                            {
                                _serverAddress = client.ServersAddress;

                                RegisterMessageProcessor(client);

                                client.Subscribe(matchGroup.Value.Select(x => x.Attribute.Name));

                                client.Listening(_pollingDelay, _cts.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            //ignore
                        }
                        catch (BrokerConnectionException e)
                        {
                            _isHealthy = false;
                            _logger.LogError(e, e.Message);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, e.Message);
                        }
                    }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
            }
            _compositeTask = Task.CompletedTask;
        }

        public void ReStart(bool force = false)
        {
            if (!IsHealthy() || force)
            {
                Pulse();

                _isHealthy = true;

                Start();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                Pulse();

                _compositeTask?.Wait(TimeSpan.FromSeconds(2));
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
            _cts?.Cancel();
        }

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            client.OnMessageReceived += async (sender, messageContext) =>
            {
                _cts.Token.ThrowIfCancellationRequested();
                Guid? operationId = null;
                try
                {
                    operationId = TracingBefore(messageContext);

                    var startTime = DateTimeOffset.UtcNow;
                    var stopwatch = Stopwatch.StartNew();

                    var message = await _serializer.DeserializeAsync(messageContext);
                    var mediumMessage = await _storage.StoreMessageAsync(message.GetName(), message.GetGroup(), message);

                    client.Commit();

                    if (operationId != null)
                    {
                        TracingAfter(operationId.Value, message, startTime, stopwatch.Elapsed);
                    }

                    _dispatcher.EnqueueToExecute(mediumMessage);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception occurred when store received message. Message:'{0}'.", messageContext);

                    client.Reject();

                    if (operationId != null)
                    {
                        TracingError(operationId.Value, messageContext, e);
                    }
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
                    _logger.LogError("Kafka client consume error. --> " + logmsg.Reason);
                    break;
                case MqLogType.ServerConnError:
                    _logger.LogCritical("Kafka server connection error. --> " + logmsg.Reason);
                    break;
                case MqLogType.ExceptionReceived:
                    _logger.LogError("AzureServiceBus subscriber received an error. --> " + logmsg.Reason);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Guid? TracingBefore(TransportMessage message)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerExtensions.CapBeforeConsume))
            {
                var operationId = Guid.NewGuid();

                var eventData = new BrokerConsumeEventData(operationId, _serverAddress, message, DateTimeOffset.UtcNow);

                s_diagnosticListener.Write(CapDiagnosticListenerExtensions.CapBeforeConsume, eventData);

                return operationId;
            }

            return null;
        }

        private void TracingAfter(Guid operationId, Message message, DateTimeOffset startTime, TimeSpan du)
        {
            //if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerExtensions.CapAfterConsume))
            //{
            //    var eventData = new BrokerConsumeEndEventData(operationId, "", _serverAddress, message, startTime, du);

            //    s_diagnosticListener.Write(CapDiagnosticListenerExtensions.CapAfterConsume, eventData);
            //}
        }

        private void TracingError(Guid operationId, TransportMessage message, Exception ex)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerExtensions.CapErrorConsume))
            {
                var eventData = new BrokerConsumeErrorEventData(operationId, _serverAddress, message, ex);
                s_diagnosticListener.Write(CapDiagnosticListenerExtensions.CapErrorConsume, eventData);
            }
        }
    }
}