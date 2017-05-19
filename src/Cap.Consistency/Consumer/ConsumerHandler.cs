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

            _serviceProvider = serviceProvider;
            _consumerInvokerFactory = consumerInvokerFactory;
            _loggerFactory = loggerFactory;
            _selector = selector;
            _logger = loggerFactory.CreateLogger<ConsumerHandler>();
        }

        public Task RouteAsync(TopicRouteContext context) {

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.ServiceProvider = _serviceProvider;

            var matchs = _selector.SelectCandidates(context);

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
