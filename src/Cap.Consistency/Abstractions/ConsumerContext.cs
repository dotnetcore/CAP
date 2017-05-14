using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;

namespace Cap.Consistency.Abstractions
{
    public class ConsumerContext
    {
        public ConsumerContext(ConsumerExecutorDescriptor descriptor, DeliverMessage message) {

            ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            DeliverMessage = message ?? throw new ArgumentNullException(nameof(message));
        }

        public ConsumerExecutorDescriptor ConsumerDescriptor { get; set; }

        public DeliverMessage DeliverMessage { get; set; }
    }
}
