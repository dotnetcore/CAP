using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Routing;

namespace Cap.Consistency.Consumer
{
    public interface IConsumerHandler<T> : ITopicRoute where T : ConsistencyMessage, new()
    {

    }
}
