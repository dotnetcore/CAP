using System;
using System.Threading;

namespace DotNetCore.CAP
{
    /// <summary>
    /// consumer client
    /// </summary>
    public interface IConsumerClient : IDisposable
    {
        void Subscribe(string topic);

        void Subscribe(string topic, int partition);

        void Listening(TimeSpan timeout, CancellationToken cancellationToken);

        void Commit();

        event EventHandler<MessageContext> OnMessageReceieved;

        event EventHandler<string> OnError;
    }
}