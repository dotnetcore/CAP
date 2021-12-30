// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Transport
{
    /// <inheritdoc />
    /// <summary>
    /// Message queue consumer client
    /// </summary>
    public interface IConsumerClient : IDisposable
    {
        BrokerAddress BrokerAddress { get; }

        /// <summary>
        /// Create (if necessary) and get topic identifiers
        /// </summary>
        /// <param name="topicNames">Names of the requested topics</param>
        /// <returns>Topic identifiers</returns>
        ICollection<string> FetchTopics(IEnumerable<string> topicNames)
        {
            return topicNames.ToList();
        }

        /// <summary>
        /// Subscribe to a set of topics to the message queue
        /// </summary>
        /// <param name="topics"></param>
        void Subscribe(IEnumerable<string> topics);

        /// <summary>
        /// Start listening
        /// </summary>
        void Listening(TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Manual submit message offset when the message consumption is complete
        /// </summary>
        void Commit(object sender);

        /// <summary>
        /// Reject message and resumption
        /// </summary>
        void Reject(object? sender);

        event EventHandler<TransportMessage> OnMessageReceived;

        event EventHandler<LogMessageEventArgs> OnLog;
    }
}