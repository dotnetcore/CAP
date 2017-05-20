using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Routing;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.Consumer
{
    public class ConsumerHandler : IConsumerHandler
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly IConsumerInvokerFactory _consumerInvokerFactory;
        private readonly IConsumerExcutorSelector _selector;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;


        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerExcutorSelector selector,
            ILoggerFactory loggerFactory) {

            _selector = selector;
            _logger = loggerFactory.CreateLogger<ConsumerHandler>();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _consumerInvokerFactory = consumerInvokerFactory;
        }

        public Task RouteAsync(TopicRouteContext context) {

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.ServiceProvider = _serviceProvider;

            var matchs = _selector.SelectCandidates(context);



            var config = new Dictionary<string, object>
            {
                { "group.id", "simple-csharp-consumer" },
                { "bootstrap.servers", brokerList }
            };

            using (var consumer = new Consumer<Null, string>(config, null, new StringDeserializer(Encoding.UTF8))) {
                //consumer.Assign(new List<TopicInfo> { new TopicInfo(topics.First(), 0, 0) });

                while (true) {
                    Message<Null, string> msg;
                    if (consumer.Consume(out msg)) {
                        Console.WriteLine($"Topic: {msg.Topic} Partition: {msg.Partition} Offset: {msg.Offset} {msg.Value}");
                    }
                }
            }



            if (matchs == null || matchs.Count == 0) {
                _logger.LogInformation("can not be fond topic route");
                return Task.CompletedTask;
            }

            var executeDescriptor = _selector.SelectBestCandidate(context, matchs);

            context.Handler = c => {

                var consumerContext = new ConsumerContext(executeDescriptor);
                var invoker = _consumerInvokerFactory.CreateInvoker(consumerContext);

                _logger.LogInformation("consumer starting");

                return invoker.InvokeAsync();
            };
            
            return Task.CompletedTask;
        }
    }
}
