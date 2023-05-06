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

namespace DotNetCore.CAP.OpenTelemetry
{
    internal class DiagnosticListener : IObserver<KeyValuePair<string, object?>>
    {
        public const string SourceName = "DotNetCore.CAP.OpenTelemetry";
        private static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");
        private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

        private readonly ConcurrentDictionary<string, ActivityContext> _contexts = new();

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
                        ActivityContext parentContext = default;

                        if (Activity.Current != null && Activity.Current.Source.Name == "OpenTelemetry.Instrumentation.AspNetCore")
                        {
                            _contexts.TryAdd(eventData.Message.GetId(), parentContext = Activity.Current.Context);
                        }
                        else
                        {
                            Activity.Current = null;
                        }

                        var activity = ActivitySource.StartActivity("Event Persistence: " + eventData.Operation, ActivityKind.Internal, parentContext);
                        if (activity != null)
                        {
                            activity.SetTag("message.name", eventData.Operation);
                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("CAP message persistence start...",
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp)));

                            if (parentContext != default)
                            {
                                _contexts[eventData.Message.GetId()] = activity.Context;
                            }
                        }
                    }
                    break;
                case CapEvents.AfterPublishMessageStore:
                    {
                        var eventData = (CapEventDataPubStore)evt.Value!;

                        if (eventData.OperationTimestamp is { } timestamp)
                            Activity.Current?.AddEvent(new ActivityEvent("CAP message persistence succeeded!",
                                DateTimeOffset.FromUnixTimeMilliseconds(timestamp),
                                new ActivityTagsCollection { new("cap.persistence.duration", eventData.ElapsedTimeMs) })
                            );

                        Activity.Current?.Stop();
                    }
                    break;
                case CapEvents.ErrorPublishMessageStore:
                    {
                        var eventData = (CapEventDataPubStore)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));
                            activity.RecordException(exception);
                            activity.Stop();
                        }
                    }
                    break;
                case CapEvents.BeforePublish:
                    {
                        var eventData = (CapEventDataPubSend)evt.Value!;
                        _contexts.TryRemove(eventData.TransportMessage.GetId(), out var context);
                        var activity = ActivitySource.StartActivity(OperateNamePrefix + eventData.Operation + ProducerOperateNameSuffix, ActivityKind.Producer, context);
                        if (activity != null)
                        {
                            activity.SetTag("messaging.system", eventData.BrokerAddress.Name);
                            activity.SetTag("messaging.destination", eventData.Operation);
                            activity.SetTag("messaging.destination_kind", "topic");
                            if (eventData.BrokerAddress.Endpoint is { } endpoint)
                                activity.SetTag("messaging.url", endpoint.Replace("-1", "5672"));
                            activity.SetTag("messaging.message_id", eventData.TransportMessage.GetId());
                            activity.SetTag("messaging.message_payload_size_bytes", eventData.TransportMessage.Body.Length);

                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("Message publishing start...",
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp)));

                            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), eventData.TransportMessage,
                                    (msg, key, value) =>
                                    {
                                        msg.Headers[key] = value;
                                    });
                        }
                    }
                    break;
                case CapEvents.AfterPublish:
                    {
                        var eventData = (CapEventDataPubSend)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("Message publishing succeeded!",
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp),
                                    new ActivityTagsCollection { new("cap.send.duration", eventData.ElapsedTimeMs) })
                                );
                            activity.Stop();
                        }
                    }
                    break;
                case CapEvents.ErrorPublish:
                    {
                        var eventData = (CapEventDataPubSend)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));
                            activity.RecordException(exception);
                            activity.Stop();
                        }
                    }
                    break;
                case CapEvents.BeforeConsume:
                    {
                        var eventData = (CapEventDataSubStore)evt.Value!;
                        var parentContext = Propagator.Extract(default, eventData.TransportMessage, (msg, key) =>
                        {
                            if (msg.Headers.TryGetValue(key, out string? value))
                            {
                                    return new[] { value };
                            }
                            return Enumerable.Empty<string>();
                        });

                        var activity = ActivitySource.StartActivity(OperateNamePrefix + eventData.Operation + ConsumerOperateNameSuffix,
                            ActivityKind.Consumer,
                            parentContext.ActivityContext);

                        if (activity != null)
                        {
                            activity.SetTag("messaging.system", eventData.BrokerAddress.Name);
                            activity.SetTag("messaging.destination", eventData.Operation);
                            activity.SetTag("messaging.destination_kind", "topic");
                            if (eventData.BrokerAddress.Endpoint is { } endpoint)
                                activity.SetTag("messaging.url", endpoint.Replace("-1", "5672"));
                            activity.SetTag("messaging.message_id", eventData.TransportMessage.GetId());
                            activity.SetTag("messaging.message_payload_size_bytes", eventData.TransportMessage.Body.Length);

                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("CAP message persistence start...",
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp)));

                            _contexts[eventData.TransportMessage.GetId() + eventData.TransportMessage.GetGroup()] = activity.Context;
                        }
                    }
                    break;
                case CapEvents.AfterConsume:
                    {
                        var eventData = (CapEventDataSubStore)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("CAP message persistence succeeded!",
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp),
                                    new ActivityTagsCollection { new("cap.receive.duration", eventData.ElapsedTimeMs) }));

                            activity.Stop();
                        }
                    }
                    break;
                case CapEvents.ErrorConsume:
                    {
                        var eventData = (CapEventDataSubStore)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));
                            activity.RecordException(exception);
                            activity.Stop();
                        }
                    }
                    break;
                case CapEvents.BeforeSubscriberInvoke:
                    {
                        var eventData = (CapEventDataSubExecute)evt.Value!;
                        _contexts.TryRemove(eventData.Message.GetId() + eventData.Message.GetGroup(), out var context);
                        var activity = ActivitySource.StartActivity("Subscriber Invoke: " + eventData.MethodInfo?.Name, ActivityKind.Internal, context);
                        if (activity != null)
                        {
                            activity.SetTag("messaging.operation", "process");
                            activity.SetTag("code.function", eventData.MethodInfo.Name);

                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("Begin invoke the subscriber:" + eventData.MethodInfo.Name,
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp)));
                        }
                    }
                    break;
                case CapEvents.AfterSubscriberInvoke:
                    {
                        var eventData = (CapEventDataSubExecute)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            if (eventData.OperationTimestamp is { } timestamp)
                                activity.AddEvent(new ActivityEvent("Subscriber invoke succeeded!",
                                    DateTimeOffset.FromUnixTimeMilliseconds(timestamp),
                                    new ActivityTagsCollection { new("cap.invoke.duration", eventData.ElapsedTimeMs) }));

                            activity.Stop();
                        }
                    }
                    break;
                case CapEvents.ErrorSubscriberInvoke:
                    {
                        var eventData = (CapEventDataSubExecute)evt.Value!;
                        if (Activity.Current is { } activity)
                        {
                            var exception = eventData.Exception!;
                            activity.SetStatus(Status.Error.WithDescription(exception.Message));
                            activity.RecordException(exception);
                            activity.Stop();
                        }
                    }
                    break;
            }
        }
    }
}