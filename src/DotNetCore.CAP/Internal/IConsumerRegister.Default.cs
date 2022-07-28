// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Internal
{
    internal class ConsumerRegister : IConsumerRegister
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);
        private readonly CapOptions _options;

        private IConsumerClientFactory _consumerClientFactory = default!;
        private IDispatcher _dispatcher = default!;
        private ISerializer _serializer = default!;
        private IDataStorage _storage = default!;

        private MethodMatcherCache _selector = default!;
        private CancellationTokenSource _cts = new();
        private BrokerAddress _serverAddress;
        private Task? _compositeTask;
        private bool _disposed;
        private bool _isHealthy = true;

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerNames.DiagnosticListenerName);

        public ConsumerRegister(ILogger<ConsumerRegister> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _options = serviceProvider.GetRequiredService<IOptions<CapOptions>>().Value;
        }

        public bool IsHealthy()
        {
            return _isHealthy;
        }

        public void Start(CancellationToken stoppingToken)
        {
            _selector = _serviceProvider.GetRequiredService<MethodMatcherCache>();
            _dispatcher = _serviceProvider.GetRequiredService<IDispatcher>();
            _serializer = _serviceProvider.GetRequiredService<ISerializer>();
            _storage = _serviceProvider.GetRequiredService<IDataStorage>();
            _consumerClientFactory = _serviceProvider.GetRequiredService<IConsumerClientFactory>();

            stoppingToken.Register(Dispose);

            Execute();
        }

        public void Execute()
        {
            var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();

            foreach (var matchGroup in groupingMatches)
            {
                ICollection<string> topics;
                try
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
                    {
                        topics = client.FetchTopics(matchGroup.Value.Select(x => x.TopicName));
                    }
                }
                catch (BrokerConnectionException e)
                {
                    _isHealthy = false;
                    _logger.LogError(e, e.Message);
                    return;
                }

                for (int i = 0; i < _options.ConsumerThreadCount; i++)
                {
                    var topicIds = topics.Select(t => t);
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            using (var client = _consumerClientFactory.Create(matchGroup.Key))
                            {
                                _serverAddress = client.BrokerAddress;

                                RegisterMessageProcessor(client);

                                client.Subscribe(topicIds);

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

                _cts = new CancellationTokenSource();
                _isHealthy = true;

                Execute();
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
            _cts.Cancel();  
            _cts.Dispose();
        }

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            // Cannot set subscription to asynchronous
            client.OnMessageReceived += (sender, transportMessage) =>
            {
                _logger.MessageReceived(transportMessage.GetId(), transportMessage.GetName());

                long? tracingTimestamp = null;
                try
                {
                    tracingTimestamp = TracingBefore(transportMessage, _serverAddress);

                    var name = transportMessage.GetName();
                    var group = transportMessage.GetGroup()!;

                    Message message;

                    var canFindSubscriber = _selector.TryGetTopicExecutor(name, group, out var executor);
                    try
                    {
                        if (!canFindSubscriber)
                        {
                            var error = $"Message can not be found subscriber. Name:{name}, Group:{group}. {Environment.NewLine} see: https://github.com/dotnetcore/CAP/issues/63";
                            var ex = new SubscriberNotFoundException(error);

                            TracingError(tracingTimestamp, transportMessage, client.BrokerAddress, ex);

                            throw ex;
                        }

                        var type = executor!.Parameters.FirstOrDefault(x => x.IsFromCap == false)?.ParameterType;
                        message = _serializer.DeserializeAsync(transportMessage, type).GetAwaiter().GetResult();
                        message.RemoveException();
                    }
                    catch (Exception e)
                    {
                        transportMessage.Headers[Headers.Exception] = e.GetType().Name + "-->" + e.Message;
                        string? dataUri;
                        if (transportMessage.Headers.TryGetValue(Headers.Type, out var val))
                        {
                            if (transportMessage.Body != null)
                            {
                                dataUri = $"data:{val};base64," + Convert.ToBase64String(transportMessage.Body);
                            }
                            else
                            {
                                dataUri = null;
                            }
                            message = new Message(transportMessage.Headers, dataUri);
                        }
                        else
                        {
                            if (transportMessage.Body != null)
                            {
                                dataUri = "data:UnknownType;base64," + Convert.ToBase64String(transportMessage.Body);
                            }
                            else
                            {
                                dataUri = null;
                            }
                            message = new Message(transportMessage.Headers, dataUri);
                        }
                    }

                    if (message.HasException())
                    {
                        var content = _serializer.Serialize(message);

                        _storage.StoreReceivedExceptionMessage(name, group, content);

                        client.Commit(sender);

                        try
                        {
                            _options.FailedThresholdCallback?.Invoke(new FailedInfo
                            {
                                ServiceProvider = _serviceProvider,
                                MessageType = MessageType.Subscribe,
                                Message = message
                            });

                            _logger.ConsumerExecutedAfterThreshold(message.GetId(), _options.FailedRetryCount);
                        }
                        catch (Exception e)
                        {
                            _logger.ExecutedThresholdCallbackFailed(e);
                        }

                        TracingAfter(tracingTimestamp, transportMessage, _serverAddress);
                    }
                    else
                    {
                        var mediumMessage = _storage.StoreReceivedMessage(name, group, message);
                        mediumMessage.Origin = message;

                        client.Commit(sender);

                        TracingAfter(tracingTimestamp, transportMessage, _serverAddress);

                        _dispatcher.EnqueueToExecute(mediumMessage, executor!);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception occurred when process received message. Message:'{0}'.", transportMessage);

                    client.Reject(sender);

                    TracingError(tracingTimestamp, transportMessage, client.BrokerAddress, e);
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
                    _isHealthy = true;
                    _logger.LogInformation("RabbitMQ consumer registered. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumerUnregistered:
                    _logger.LogWarning("RabbitMQ consumer unregistered. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumerShutdown:
                    _isHealthy = false;
                    _logger.LogWarning("RabbitMQ consumer shutdown. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumeError:
                    _logger.LogError("Kafka client consume error. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConsumeRetries:
                    _logger.LogWarning("Kafka client consume exception, retying... --> " + logmsg.Reason);
                    break;
                case MqLogType.ServerConnError:
                    _isHealthy = false;
                    _logger.LogCritical("Kafka server connection error. --> " + logmsg.Reason);
                    break;
                case MqLogType.ExceptionReceived:
                    _logger.LogError("AzureServiceBus subscriber received an error. --> " + logmsg.Reason);
                    break;
                case MqLogType.AsyncErrorEvent:
                    _logger.LogError("NATS subscriber received an error. --> " + logmsg.Reason);
                    break;
                case MqLogType.ConnectError:
                    _isHealthy = false;
                    _logger.LogError("NATS server connection error. -->  " + logmsg.Reason);
                    break;
                case MqLogType.InvalidIdFormat:
                    _logger.LogError("AmazonSQS subscriber delete inflight message failed, invalid id. --> " + logmsg.Reason);
                    break;
                case MqLogType.MessageNotInflight:
                    _logger.LogError("AmazonSQS subscriber change message's visibility failed, message isn't in flight. --> " + logmsg.Reason);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region tracing

        private long? TracingBefore(TransportMessage message, BrokerAddress broker)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.BeforeConsume))
            {
                var eventData = new CapEventDataSubStore()
                {
                    OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Operation = message.GetName(),
                    BrokerAddress = broker,
                    TransportMessage = message
                };

                s_diagnosticListener.Write(CapDiagnosticListenerNames.BeforeConsume, eventData);

                return eventData.OperationTimestamp;
            }

            return null;
        }

        private void TracingAfter(long? tracingTimestamp, TransportMessage message, BrokerAddress broker)
        {
            if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterConsume))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var eventData = new CapEventDataSubStore()
                {
                    OperationTimestamp = now,
                    Operation = message.GetName(),
                    BrokerAddress = broker,
                    TransportMessage = message,
                    ElapsedTimeMs = now - tracingTimestamp.Value
                };

                s_diagnosticListener.Write(CapDiagnosticListenerNames.AfterConsume, eventData);
            }
        }

        private void TracingError(long? tracingTimestamp, TransportMessage message, BrokerAddress broker, Exception ex)
        {
            if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorConsume))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var eventData = new CapEventDataSubStore()
                {
                    OperationTimestamp = now,
                    Operation = message.GetName(),
                    BrokerAddress = broker,
                    TransportMessage = message,
                    ElapsedTimeMs = now - tracingTimestamp.Value,
                    Exception = ex
                };

                s_diagnosticListener.Write(CapDiagnosticListenerNames.ErrorConsume, eventData);
            }
        }

        #endregion
    }
}