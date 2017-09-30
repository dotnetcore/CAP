using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetCore.CAP
{
    /// <summary>
    /// consumer client
    /// </summary>
    public interface IConsumerClient : IDisposable
    {
        void Subscribe(IEnumerable<string> topics);

        void Listening(TimeSpan timeout, CancellationToken cancellationToken);

        void Commit();

        void Reject();

        event EventHandler<MessageContext> OnMessageReceived;

        event EventHandler<string> OnError;
    }
}