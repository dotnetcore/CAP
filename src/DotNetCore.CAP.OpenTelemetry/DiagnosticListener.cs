﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotNetCore.CAP.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using CapEvents = DotNetCore.CAP.Diagnostics.CapDiagnosticListenerNames;

namespace DotNetCore.CAP.OpenTelemetry;

internal class DiagnosticListener : IObserver<KeyValuePair<string, object?>>
{
    public const string SourceName = "DotNetCore.CAP.OpenTelemetry";

    private const string OperateNamePrefix = "CAP/";
    private const string ProducerOperateNameSuffix = "/Publisher";
    private const string ConsumerOperateNameSuffix = "/Subscriber";
    private static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

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
                    ActivityContext parentContext = Propagator.Extract(default, eventData.Message, (msg, key) =>
                    {
                        if (msg.Headers.TryGetValue(key, out var value) && value != null) return new string[] { value };
                        return Enumerable.Empty<string>();
                    }).ActivityContext;

                    if (parentContext == default)
                    {
                        parentContext = Activity.Current?.Context ?? default;
                    }
                    var activity = ActivitySource.StartActivity("Event Persistence: " + eventData.Operation,
                        ActivityKind.Internal, parentContext);
                    if (activity != null)
                    {
                        activity.SetTag("messaging.destination.name", eventData.Operation);
                        activity.AddEvent(new ActivityEvent("CAP message persistence start...",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                        if (parentContext != default && Activity.Current != null)
                        {
                            Propagator.Inject(new PropagationContext(Activity.Current.Context, Baggage.Current),
                                eventData.Message,
                                (msg, key, value) => { msg.Headers[key] = value; });
                        }
                        ;
                    }
                }
                break;
            case CapEvents.AfterPublishMessageStore:
                {
                    var eventData = (CapEventDataPubStore)evt.Value!;

                    Activity.Current?.AddEvent(new ActivityEvent("CAP message persistence succeeded!",
                        DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
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
                        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                        activity.AddException(exception);
                        activity.Stop();
                    }
                }
                break;
            case CapEvents.BeforePublish:
                {
                    var eventData = (CapEventDataPubSend)evt.Value!;
                    var parentContext = Propagator.Extract(default, eventData.TransportMessage, (msg, key) =>
                    {
                        if (msg.Headers.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                        {
                            return new string[] { value };
                        }

                        return Enumerable.Empty<string>();
                    });

                    var activity = ActivitySource.StartActivity(
                        OperateNamePrefix + eventData.Operation + ProducerOperateNameSuffix, ActivityKind.Producer,
                        parentContext.ActivityContext);
                    if (activity != null)
                    {
                        activity.SetTag("messaging.system", eventData.BrokerAddress.Name);
                        activity.SetTag("messaging.message.id", eventData.TransportMessage.GetId());
                        activity.SetTag("messaging.message.body.size", eventData.TransportMessage.Body.Length);
                        activity.SetTag("messaging.message.conversation_id", eventData.TransportMessage.GetCorrelationId());
                        activity.SetTag("messaging.destination.name", eventData.Operation);
                        if (eventData.BrokerAddress.Endpoint is { } endpoint)
                        {
                            var parts = endpoint.Split(':');
                            if (parts.Length > 0)
                            {
                                activity.SetTag("server.address", parts[0]);
                            }
                            if (parts.Length > 1 && int.TryParse(parts[1], out var port))
                            {
                                activity.SetTag("server.port", port);
                            }
                        }
                        activity.AddEvent(new ActivityEvent("Message publishing start...",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current),
                            eventData.TransportMessage,
                            (msg, key, value) => { msg.Headers[key] = value; });
                    }
                }
                break;
            case CapEvents.AfterPublish:
                {
                    var eventData = (CapEventDataPubSend)evt.Value!;
                    if (Activity.Current is { } activity)
                    {
                        activity.AddEvent(new ActivityEvent("Message publishing succeeded!",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
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
                        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                        activity.AddException(exception);
                        activity.Stop();
                    }
                }
                break;
            case CapEvents.BeforeConsume:
                {
                    var eventData = (CapEventDataSubStore)evt.Value!;
                    var parentContext = Propagator.Extract(default, eventData.TransportMessage, (msg, key) =>
                    {
                        if (msg.Headers.TryGetValue(key, out var value) && value != null) return new string[] { value };
                        return Enumerable.Empty<string>();
                    });

                    Baggage.Current = parentContext.Baggage;
                    var activity = ActivitySource.StartActivity(
                        OperateNamePrefix + eventData.Operation + ConsumerOperateNameSuffix,
                        ActivityKind.Consumer,
                        parentContext.ActivityContext);

                    if (activity != null)
                    {
                        activity.SetTag("messaging.system", eventData.BrokerAddress.Name);
                        activity.SetTag("messaging.message.id", eventData.TransportMessage.GetId());
                        activity.SetTag("messaging.message.body.size", eventData.TransportMessage.Body.Length);
                        activity.SetTag("messaging.operation.type", "receive");
                        activity.SetTag("messaging.client.id", eventData.TransportMessage.GetExecutionInstanceId());
                        activity.SetTag("messaging.destination.name", eventData.Operation);
                        activity.SetTag("messaging.consumer.group.name", eventData.TransportMessage.GetGroup());
                        if (eventData.BrokerAddress.Endpoint is { } endpoint)
                        {
                            var parts = endpoint.Split(':');
                            if (parts.Length > 0)
                            {
                                activity.SetTag("server.address", parts[0]);
                            }
                            if (parts.Length > 1 && int.TryParse(parts[1], out var port))
                            {
                                activity.SetTag("server.port", port);
                            }
                        }
                        activity.AddEvent(new ActivityEvent("CAP message persistence start...",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));
                    }
                }
                break;
            case CapEvents.AfterConsume:
                {
                    var eventData = (CapEventDataSubStore)evt.Value!;
                    if (Activity.Current is { } activity)
                    {
                        activity.AddEvent(new ActivityEvent("CAP message persistence succeeded!",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
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
                        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                        activity.AddException(exception);
                        activity.Stop();
                    }
                }
                break;
            case CapEvents.BeforeSubscriberInvoke:
                {
                    ActivityContext context = default;
                    var eventData = (CapEventDataSubExecute)evt.Value!;
                    var propagatedContext = Propagator.Extract(default, eventData.Message, (msg, key) =>
                    {
                        if (msg.Headers.TryGetValue(key, out var value) && value != null) return new string[] { value };
                        return Enumerable.Empty<string>();
                    });

                    if (propagatedContext != default)
                    {
                        context = propagatedContext.ActivityContext;
                        Baggage.Current = propagatedContext.Baggage;
                    }

                    var activity = ActivitySource.StartActivity("Subscriber Invoke: " + eventData.MethodInfo!.Name,
                        ActivityKind.Internal,
                        context);

                    if (activity != null)
                    {
                        activity.SetTag("code.function.name", eventData.MethodInfo.Name);

                        activity.AddEvent(new ActivityEvent("Begin invoke the subscriber:" + eventData.MethodInfo.Name,
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));
                    }
                }
                break;
            case CapEvents.AfterSubscriberInvoke:
                {
                    var eventData = (CapEventDataSubExecute)evt.Value!;
                    if (Activity.Current is { } activity)
                    {
                        activity.AddEvent(new ActivityEvent("Subscriber invoke succeeded!",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
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
                        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                        activity.AddException(exception);
                        activity.Stop();
                    }
                }
                break;
        }
    }
}
