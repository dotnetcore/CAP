using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cap.Consistency.Internal;

namespace Cap.Consistency.Consumer
{
    public class ConsumerHandler<T> : IConsumerHandler<T> where T : ConsistencyMessage, new()
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConsumerInvokerFactory _consumerInvokerFactory;
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly MethodMatcherCache _selector;
        private readonly ConsistencyOptions _options;
        private readonly ConsistencyMessageManager<T> _messageManager;

        public event EventHandler<T> MessageReceieved;

        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerClientFactory consumerClientFactory,
            ILoggerFactory loggerFactory,
            ConsistencyMessageManager<T> messageManager,
            MethodMatcherCache selector,
            IOptions<ConsistencyOptions> options) {

            _selector = selector;
            _logger = loggerFactory.CreateLogger<ConsumerHandler<T>>();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _consumerInvokerFactory = consumerInvokerFactory;
            _consumerClientFactory = consumerClientFactory;
            _options = options.Value;
            _messageManager = messageManager;
        }


        protected virtual void OnMessageReceieved(T message) {
            MessageReceieved?.Invoke(this, message);
        }

        public Task RouteAsync(TopicRouteContext context) {

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.ServiceProvider = _serviceProvider;

            var matchs = _selector.GetCandidatesMethods(context);

            var groupingMatchs = matchs.GroupBy(x => x.Value.GroupId);

            foreach (var matchGroup in groupingMatchs) {
                using (var client = _consumerClientFactory.Create(matchGroup.Key, _options.BrokerUrlList)) {
                    client.MessageReceieved += OnMessageReceieved;

                    foreach (var item in matchGroup) {
                        client.Subscribe(item.Key, item.Value.Topic.Partition);
                    }

                    client.Listening(TimeSpan.Zero);
                }
            }
            return Task.CompletedTask;
        }

        private void OnMessageReceieved(object sender, DeliverMessage message) {
            T consistencyMessage = new T() {
                Id = message.MessageKey,
                Payload = Encoding.UTF8.GetString(message.Body)
            };

            _logger.LogInformation("message receieved message topic name: " + consistencyMessage.Id);

            _messageManager.CreateAsync(consistencyMessage);

            try {
                var executeDescriptor = _selector.GetTopicExector(message.MessageKey);

                var consumerContext = new ConsumerContext(executeDescriptor, message);

                var invoker = _consumerInvokerFactory.CreateInvoker(consumerContext);

                invoker.InvokeAsync();

                _messageManager.UpdateAsync(consistencyMessage);

            }
            catch (Exception ex) {

                _logger.LogError("exception raised when excute method : " + ex.Message);

                throw ex;
            }
        }
    }
}
