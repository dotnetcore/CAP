using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Infrastructure;

namespace Cap.Consistency.Consumer
{
    public interface IConsumerClient : IDisposable
    {
        void Subscribe(string topic);

        void Subscribe(string topic, int partition);

        void Listening(TimeSpan timeout);

        event EventHandler<DeliverMessage> MessageReceieved;
    }
}
