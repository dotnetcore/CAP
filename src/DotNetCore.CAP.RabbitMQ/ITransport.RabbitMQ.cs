// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
     internal sealed class RabbitMQTransport : ITransport
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly ILogger _logger;
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(ITransport));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        private readonly string _exchange;

        public RabbitMQTransport(
            ILogger<RabbitMQTransport> logger,
            IConnectionChannelPool connectionChannelPool)
        {
            _logger = logger;
            _connectionChannelPool = connectionChannelPool;
            _exchange = _connectionChannelPool.Exchange;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", _connectionChannelPool.HostAddress);

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            IModel channel = null;
            try
            {
                var activityName = message.GetName();

                using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer))
                {
                    channel = _connectionChannelPool.Rent();

                    channel.ConfirmSelect();

                    var props = channel.CreateBasicProperties();
                    props.DeliveryMode = 2;
                    props.Headers = message.Headers.ToDictionary(x => x.Key, x => (object) x.Value);
                    
                    ActivityContext contextToInject = default;
                    if (activity != null)
                    {
                        contextToInject = activity.Context;
                    }
                    else if (Activity.Current != null)
                    {
                        contextToInject = Activity.Current.Context;
                    }

                    Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props, InjectTraceContextIntoBasicProperties);

                    AddMessagingTags(activity, message);
                    
                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                    channel.ExchangeDeclare(_exchange, RabbitMQOptions.ExchangeType, true);

                    channel.BasicPublish(_exchange, message.GetName(), props, message.Body);

                    _logger.LogDebug($"RabbitMQ topic message [{message.GetName()}] has been published.");

                    return Task.FromResult(OperateResult.Success);
                }
            }
            catch (System.Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
                var errors = new OperateError
                {
                    Code = ex.HResult.ToString(),
                    Description = ex.Message
                };

                return Task.FromResult(OperateResult.Failed(wrapperEx, errors));
            }
            finally
            {
                if (channel != null)
                {
                    _connectionChannelPool.Return(channel);
                }
            }
        }
        
        private void AddMessagingTags(Activity activity, TransportMessage message)
        {
            activity?.SetTag("message_id", message.GetId());
            activity?.SetTag("correlation_id", message.GetCorrelationId());
            activity?.SetTag("messaging_system", "rabbitmq");
            activity?.SetTag("destination_kind", "queue");
            activity?.SetTag("exchange_name", _exchange);
            activity?.SetTag("routing_key", message.GetName());
        }
        
        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
            props.Headers ??= new Dictionary<string, object>();

            props.Headers[key] = value;
        }
    }
}