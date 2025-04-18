// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Transport;

/// <inheritdoc />
/// <summary>
/// Message queue consumer client interface that defines operations for consuming messages from various message brokers
/// </summary>
public interface IConsumerClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the broker address information that this consumer is connected to
    /// </summary>
    BrokerAddress BrokerAddress { get; }

    /// <summary>
    /// Creates (if necessary) and retrieves topic identifiers from the message broker
    /// </summary>
    /// <param name="topicNames">Names of the requested topics to fetch</param>
    /// <returns>A collection of topic identifiers returned by the broker</returns>
    Task<ICollection<string>> FetchTopicsAsync(IEnumerable<string> topicNames)
    {
        return Task.FromResult<ICollection<string>>(topicNames.ToList());
    }

    /// <summary>
    /// Subscribes to a set of topics in the message broker
    /// </summary>
    /// <param name="topics">Collection of topic identifiers to subscribe to</param>
    /// <returns>A task that represents the asynchronous subscribe operation</returns>
    Task SubscribeAsync(IEnumerable<string> topics);

    /// <summary>
    /// Starts listening for messages from the subscribed topics
    /// </summary>
    /// <param name="timeout">Maximum time to wait when polling for messages</param>
    /// <param name="cancellationToken">Token to cancel the listening operation</param>
    /// <returns>A task that represents the asynchronous listening operation</returns>
    Task ListeningAsync(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Manually commits message offset when the message consumption is complete
    /// </summary>
    /// <param name="sender">The message or context object to commit</param>
    /// <returns>A task that represents the asynchronous commit operation</returns>
    Task CommitAsync(object? sender);

    /// <summary>
    /// Rejects the message and optionally returns it to the queue for reprocessing
    /// </summary>
    /// <param name="sender">The message or context object to reject</param>
    /// <returns>A task that represents the asynchronous reject operation</returns>
    Task RejectAsync(object? sender);

    /// <summary>
    /// Callback that is invoked when a message is received from the broker
    /// </summary>
    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    /// <summary>
    /// Callback that is invoked when logging events occur in the consumer client
    /// </summary>
    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }
}
