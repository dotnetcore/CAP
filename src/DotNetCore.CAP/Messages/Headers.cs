// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Messages;

/// <summary>
/// Defines the standard header names used in CAP messages.
/// These headers carry metadata and system information that controls message routing, tracking, and processing.
/// </summary>
public static class Headers
{
    /// <summary>
    /// Unique identifier for the message.
    /// Can be set explicitly when publishing a message, or automatically assigned by CAP.
    /// This ID is used to track and correlate messages throughout their lifecycle.
    /// Value: "cap-msg-id"
    /// </summary>
    public const string MessageId = "cap-msg-id";

    /// <summary>
    /// The topic or message name that identifies what kind of message this is.
    /// Used for routing to the correct subscribers.
    /// Value: "cap-msg-name"
    /// </summary>
    public const string MessageName = "cap-msg-name";

    /// <summary>
    /// The consumer group that should receive this message.
    /// In Kafka, this maps to the consumer group; in RabbitMQ, it maps to the queue name.
    /// Value: "cap-msg-group"
    /// </summary>
    public const string Group = "cap-msg-group";

    /// <summary>
    /// The .NET type name of the message value/payload.
    /// Used during deserialization to reconstruct the original object type.
    /// Value: "cap-msg-type"
    /// </summary>
    public const string Type = "cap-msg-type";

    /// <summary>
    /// Correlation ID for linking related messages in a message flow or saga pattern.
    /// Allows tracing a chain of messages across different topics and services.
    /// Value: "cap-corr-id"
    /// </summary>
    public const string CorrelationId = "cap-corr-id";

    /// <summary>
    /// Sequence number for ordering correlated messages.
    /// Indicates the position of this message in a correlated sequence.
    /// Value: "cap-corr-seq"
    /// </summary>
    public const string CorrelationSequence = "cap-corr-seq";

    /// <summary>
    /// Name of the subscriber callback handler that should process the response to this message.
    /// Used in request-response patterns where a subscriber needs to send a reply.
    /// Value: "cap-callback-name"
    /// </summary>
    public const string CallbackName = "cap-callback-name";

    /// <summary>
    /// Identifier of the application instance that executed or is executing the message.
    /// Useful in distributed systems to track which instance processed a message.
    /// Value: "cap-exec-instance-id"
    /// </summary>
    public const string ExecutionInstanceId = "cap-exec-instance-id";

    /// <summary>
    /// Timestamp indicating when the message was sent/published, in UTC ISO 8601 format.
    /// Value: "cap-senttime"
    /// </summary>
    public const string SentTime = "cap-senttime";

    /// <summary>
    /// Timestamp indicating when a delayed message should be published, in UTC ISO 8601 format.
    /// This header is only present for messages scheduled for delayed delivery.
    /// Value: "cap-delaytime"
    /// </summary>
    public const string DelayTime = "cap-delaytime";

    /// <summary>
    /// Exception information if the message processing failed.
    /// Contains the exception type name and message formatted as "ExceptionTypeName-->ExceptionMessage".
    /// Value: "cap-exception"
    /// </summary>
    public const string Exception = "cap-exception";

    /// <summary>
    /// W3C Trace Context parent trace ID for distributed tracing and OpenTelemetry integration.
    /// Enables correlation of messages with the broader application trace.
    /// Value: "traceparent"
    /// </summary>
    public const string TraceParent = "traceparent"; 
}