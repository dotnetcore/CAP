using System;
using System.Collections.Generic;
using System.Text;

namespace Cap.Consistency.Abstractions
{
    public class ConsumerInvokerContext
    {
        public ConsumerInvokerContext(ConsumerContext consumerContext) {
            ConsumerContext = consumerContext ?? 
                throw new ArgumentNullException(nameof(consumerContext));

        }

        public ConsumerContext ConsumerContext { get; set; }

        public IConsumerInvoker Result { get; set; }

    }
}
