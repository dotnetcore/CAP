using System;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP
{
    /// <summary>
    /// consumer client
    /// </summary>
    public interface IConsumerClient : IDisposable
    {
        void Subscribe(string topic);

        void Subscribe(string topic, int partition);

        void Listening(TimeSpan timeout);

        event EventHandler<MessageContext> MessageReceieved;
    }
}