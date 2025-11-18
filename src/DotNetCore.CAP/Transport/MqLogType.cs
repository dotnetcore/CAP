// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Transport;

/// <summary>
/// Defines log event types reported by message brokers and transport implementations.
/// These events allow applications to monitor broker and consumer health, connectivity issues, and other diagnostics.
/// </summary>
public enum MqLogType
{
    /// <summary>
    /// Consumer subscription was cancelled (RabbitMQ).
    /// Typically indicates the consumer was unsubscribed or the connection was terminated.
    /// </summary>
    ConsumerCancelled,

    /// <summary>
    /// Consumer successfully registered and is ready to receive messages (RabbitMQ).
    /// </summary>
    ConsumerRegistered,

    /// <summary>
    /// Consumer unregistered from the message broker (RabbitMQ).
    /// </summary>
    ConsumerUnregistered,

    /// <summary>
    /// Consumer connection to the broker was shut down (RabbitMQ).
    /// </summary>
    ConsumerShutdown,

    /// <summary>
    /// An error occurred during message consumption (Kafka).
    /// </summary>
    ConsumeError,

    /// <summary>
    /// Consumer is retrying after a consumption failure (Kafka).
    /// </summary>
    ConsumeRetries,

    /// <summary>
    /// Failed to establish or maintain connection to the broker server (Kafka).
    /// </summary>
    ServerConnError,

    /// <summary>
    /// An exception was received from the message broker (Azure Service Bus).
    /// </summary>
    ExceptionReceived,

    /// <summary>
    /// An asynchronous error event occurred (NATS).
    /// </summary>
    AsyncErrorEvent,

    /// <summary>
    /// Failed to connect or connection error occurred (NATS).
    /// </summary>
    ConnectError,

    /// <summary>
    /// An invalid ID format was detected during message processing (Amazon SQS).
    /// </summary>
    InvalidIdFormat,

    /// <summary>
    /// A message is not currently in flight, preventing visibility timeout change (Amazon SQS).
    /// </summary>
    MessageNotInflight,

    /// <summary>
    /// An error occurred during message consumption (Redis Streams).
    /// </summary>
    RedisConsumeError
}

/// <summary>
/// Contains event arguments for message broker log events.
/// These events are used to notify subscribers about broker health, connectivity, and operational status.
/// </summary>
public class LogMessageEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the reason or detailed description of the log event.
    /// This typically contains error messages, consumer IDs, or other contextual information.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the type of log event that occurred (e.g., ConsumerCancelled, ServerConnError, etc.).
    /// </summary>
    public MqLogType LogType { get; set; }
}