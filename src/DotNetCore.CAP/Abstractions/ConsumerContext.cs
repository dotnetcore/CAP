using System;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Abstractions
{
    public class ConsumerContext
    {
        public ConsumerContext(ConsumerExecutorDescriptor descriptor, DeliverMessage message)
        {
            ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            DeliverMessage = message ?? throw new ArgumentNullException(nameof(message));
        }

        public ConsumerExecutorDescriptor ConsumerDescriptor { get; set; }

        public DeliverMessage DeliverMessage { get; set; }
    }
}