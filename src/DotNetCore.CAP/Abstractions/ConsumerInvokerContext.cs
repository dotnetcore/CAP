using System;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// a context of consumer invoker.
    /// </summary>
    public class ConsumerInvokerContext
    {
        public ConsumerInvokerContext(ConsumerContext consumerContext)
        {
            ConsumerContext = consumerContext ??
                              throw new ArgumentNullException(nameof(consumerContext));
        }

        public ConsumerContext ConsumerContext { get; set; }

        public IConsumerInvoker Result { get; set; }
    }
}