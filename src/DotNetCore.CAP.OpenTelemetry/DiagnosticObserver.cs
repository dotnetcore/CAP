// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Messages;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using CapEvents = DotNetCore.CAP.Diagnostics.CapDiagnosticListenerNames;
using Status = OpenTelemetry.Trace.Status;

namespace DotNetCore.CAP.OpenTelemetry
{
    internal class CapDiagnosticObserver : IObserver<KeyValuePair<string, object?>>
    {
        private static readonly ActivitySource ActivitySource = new("DotNetCore.CAP.OpenTelemetry");
        private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

        private readonly ConcurrentDictionary<string, Activity> _contexts = new();

        private const string OperateNamePrefix = "CAP/";
        private const string ProducerOperateNameSuffix = "/Publisher";
        private const string ConsumerOperateNameSuffix = "/Subscriber";

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> evt)
        {
            switch (evt.Key)
            {
                case CapEvents.BeforePublishMessageStore:
                    {
                        var eventData = (CapEventDataPubStore)evt.Value!;
                        var activity = ActivitySource.StartActivity("Event Persistence: " + eventData.Operation);
                        if (activity != null)
                        {
                            activity.SetTag("component", "CAP");
                            activity.SetTag("message.name", eventData.Operation);
                            activity.AddEvent(new ActivityEvent("CAP message persistence start...",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));
                            _contexts.TryAdd(eventData.Message.GetId(), activity);
                        }
                    }
                    break;
                case CapEvents.AfterPublishMessageStore:
                    {
                        var eventData = (CapEventDataPubStore)evt.Value!;
                        if (_contexts.TryRemove(eventData.Message.GetId(), out Activity activity))
                        {
                            activity.AddEvent(new ActivityEvent("CAP message persistence succeeded!",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
                                new ActivityTagsCollection { new("cap.persistence.duration", eventData.ElapsedTimeMs) })
                            );
                            activity.Dispose();
                        }
                    }
                    break;
                case CapEvents.ErrorPublishMessageStore:
                    {
                        var eventData = (CapEventDataPubStore)evt.Value!;
                        if (_contexts.TryRemove(eventData.Message.GetId(), out Activity activity))
                        {
                            var exception = eventData.Exception!;

                            activity.SetStatus(Status.Error.WithDescription(exception.Message));

                            activity.RecordException(exception);

                            activity.Dispose();
                        }
                    }
                    break;
                case CapEvents.BeforePublish:
                    {
                        var eventData = (CapEventDataPubSend)evt.Value!;
                        var activity = ActivitySource.StartActivity(OperateNamePrefix + eventData.Operation + ProducerOperateNameSuffix, ActivityKind.Producer);
                        if (activity != null)
                        {
                            activity.SetTag("messaging.system", eventData.BrokerAddress.Name);
                            activity.SetTag("messaging.destination", eventData.Operation);
                            activity.SetTag("messaging.destination_kind", "topic");
                            activity.SetTag("messaging.url", eventData.BrokerAddress.Endpoint!.Replace("-1", "5672"));
                            activity.SetTag("messaging.message_id", eventData.TransportMessage.GetId());
                            activity.SetTag("messaging.message_payload_size_bytes", eventData.TransportMessage.Body?.Length);

                            activity.AddEvent(new ActivityEvent("Message publishing start...",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), eventData.TransportMessage,
                                (msg, key, value) =>
                                {
                                    msg.Headers[key] = value;
                                });

                            _contexts.TryAdd(eventData.TransportMessage.GetId(), activity);
                        }
                    }
                    break;
                case CapEvents.AfterPublish:
                    {
                        var eventData = (CapEventDataPubSend)evt.Value!;
                        if (_contexts.TryRemove(eventData.TransportMessage.GetId(), out Activity activity))
                        {
                            activity.AddEvent(new ActivityEvent("Message publishing succeeded!",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
                                new ActivityTagsCollection { new("cap.send.duration", eventData.ElapsedTimeMs) })
                            );
                            activity.Dispose();
                        }
                    }
                    break;
                case CapEvents.ErrorPublish:
                    {
                        var eventData = (CapEventDataPubSend)evt.Value!;
                        if (_contexts.TryRemove(eventData.TransportMessage.GetId(), out Activity activity))
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));

                            activity.RecordException(exception);

                            activity.Dispose();
                        }
                    }
                    break;
                case CapEvents.BeforeConsume:
                    {

                        var eventData = (CapEventDataSubStore)evt.Value!;
                        var activity = ActivitySource.StartActivity(OperateNamePrefix + eventData.Operation + ConsumerOperateNameSuffix, ActivityKind.Consumer);
                        if (activity != null)
                        {
                            var parentContext = Propagator.Extract(default, eventData.TransportMessage, (msg, key) =>
                            {
                                if (msg.Headers.TryGetValue(key, out string? value))
                                {
                                    return new[] { value };
                                }
                                return Enumerable.Empty<string>();
                            });

                            Baggage.Current = parentContext.Baggage;

                            activity.SetTag("messaging.system", eventData.BrokerAddress.Name);
                            activity.SetTag("messaging.destination", eventData.Operation);
                            activity.SetTag("messaging.destination_kind", "topic");
                            activity.SetTag("messaging.url", eventData.BrokerAddress.Endpoint!.Replace("-1", "5672"));
                            activity.SetTag("messaging.message_id", eventData.TransportMessage.GetId());
                            activity.SetTag("messaging.message_payload_size_bytes", eventData.TransportMessage.Body?.Length);

                            activity.AddEvent(new ActivityEvent("CAP message persistence start...",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                            _contexts.TryAdd(eventData.TransportMessage.GetId(), activity);

                        }
                    }
                    break;
                case CapEvents.AfterConsume:
                    {
                        var eventData = (CapEventDataSubStore)evt.Value!;
                        if (_contexts.TryRemove(eventData.TransportMessage.GetId(), out Activity activity))
                        {
                            activity.AddEvent(new ActivityEvent("CAP message persistence succeeded!",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
                                new ActivityTagsCollection { new("cap.receive.duration", eventData.ElapsedTimeMs) }));

                            _contexts.TryAdd(eventData.TransportMessage.GetId(), activity);
                        }
                    }
                    break;
                case CapEvents.ErrorConsume:
                    {
                        var eventData = (CapEventDataSubStore)evt.Value!;
                        if (_contexts.TryRemove(eventData.TransportMessage.GetId(), out Activity activity))
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));

                            activity.RecordException(exception);

                            activity.Dispose();
                        }
                    }
                    break;
                case CapEvents.BeforeSubscriberInvoke:
                    {
                        var eventData = (CapEventDataSubExecute)evt.Value!;
                        var activity = ActivitySource.StartActivity("Subscriber Invoke: " + eventData.MethodInfo!.Name);
                        if (activity != null)
                        {
                            activity.SetTag("component", "CAP");
                            activity.SetTag("messaging.operation", "process");
                            activity.SetTag("code.function", eventData.MethodInfo.Name);
                            activity.SetTag("code.namespace", eventData.MethodInfo.GetType().Namespace);

                            activity.AddEvent(new ActivityEvent("Begin invoke the subscriber:" + eventData.MethodInfo.Name,
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                            _contexts.TryAdd(eventData.Message.GetId(), activity);
                        }
                    }
                    break;
                case CapEvents.AfterSubscriberInvoke:
                    {
                        var eventData = (CapEventDataSubExecute)evt.Value!;
                        if (_contexts.TryRemove(eventData.Message.GetId(), out Activity activity))
                        {
                            activity.AddEvent(new ActivityEvent("Subscriber invoke succeeded!",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
                                new ActivityTagsCollection { new("cap.invoke.duration", eventData.ElapsedTimeMs) }));
                             
                            activity.Dispose();
                        }

                    }
                    break;
                case CapEvents.ErrorSubscriberInvoke:
                    {
                        var eventData = (CapEventDataSubExecute)evt.Value!;
                        if (_contexts.TryRemove(eventData.Message.GetId(), out Activity activity))
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));
                            activity.RecordException(exception);
                            activity.Dispose();
                        }
                    }
                    break;

            }
        }

    }
}