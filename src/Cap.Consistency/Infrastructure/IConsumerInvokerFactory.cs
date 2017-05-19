using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency.Infrastructure
{
    public interface IConsumerInvokerFactory
    {
        IConsumerInvoker CreateInvoker(ConsumerContext actionContext);
    }
}
