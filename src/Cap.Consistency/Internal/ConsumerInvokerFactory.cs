using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Infrastructure;

namespace Cap.Consistency.Internal
{
    public class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        public IConsumerInvoker CreateInvoker(ConsumerContext consumerContext) {
            var context = new ConsumerInvokerContext(consumerContext);
            return context.Result;
        }
    }
}
