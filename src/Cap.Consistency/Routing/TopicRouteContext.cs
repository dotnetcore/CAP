using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Consumer;
using Cap.Consistency.Infrastructure;

namespace Cap.Consistency.Routing
{
    public delegate Task HandlerConsumer(ConsumerExecutorDescriptor context);

    public class TopicRouteContext
    {
        public TopicRouteContext() {

        }

        public TopicRouteContext(DeliverMessage message) {
            Message = message;
        }

        public DeliverMessage Message { get; }

        // public event EventHandler<ConsumerExecutorDescriptor> OnMessage;

        public HandlerConsumer Handler { get; set; }

        public IList<IConsumerHandler> Consumers { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public IList<ITopicRoute> Routes { get; set; }

    }
}
