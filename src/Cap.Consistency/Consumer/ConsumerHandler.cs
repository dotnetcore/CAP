using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cap.Consistency.Store;

namespace Cap.Consistency.Consumer
{
    public class ConsumerHandler : IConsumerHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConsumerInvokerFactory _consumerInvokerFactory;
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly MethodMatcherCache _selector;
        private readonly ConsistencyOptions _options;
        private readonly ConsistencyMessageManager _messageManager;

        public event EventHandler<ConsistencyMessage> MessageReceieved;

        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerClientFactory consumerClientFactory,
            ILoggerFactory loggerFactory,
            ConsistencyMessageManager messageManager,
            MethodMatcherCache selector,
            IOptions<ConsistencyOptions> options) {

            _selector = selector;
            _logger = loggerFactory.CreateLogger<ConsumerHandler>();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _consumerInvokerFactory = consumerInvokerFactory;
            _consumerClientFactory = consumerClientFactory;
            _options = options.Value;
            _messageManager = messageManager;
        }


        protected virtual void OnMessageReceieved(ConsistencyMessage message) {
            MessageReceieved?.Invoke(this, message);
        }

        public Task RouteAsync(TopicRouteContext context) {

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.ServiceProvider = _serviceProvider;

            var matchs = _selector.GetCandidatesMethods(context);

            var groupingMatchs = matchs.GroupBy(x => x.Value.Attribute.GroupOrExchange);

            foreach (var matchGroup in groupingMatchs) {
                Task.Factory.StartNew(() => {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key, _options.BrokerUrlList)) {
                        client.MessageReceieved += OnMessageReceieved;

                        foreach (var item in matchGroup) {
                            client.Subscribe(item.Key);
                        }

                        client.Listening(TimeSpan.FromSeconds(1));
                    }
                }, TaskCreationOptions.LongRunning);
            }
            return Task.CompletedTask;
        }

        private void OnMessageReceieved(object sender, DeliverMessage message) {
            var consistencyMessage = new ConsistencyMessage() {
                Id = message.MessageKey,
                Payload = Encoding.UTF8.GetString(message.Body)
            };

            _logger.LogInformation("message receieved message topic name: " + consistencyMessage.Id);

            _messageManager.CreateAsync(consistencyMessage).Wait();

            try {
                var executeDescriptor = _selector.GetTopicExector(message.MessageKey);

                var consumerContext = new ConsumerContext(executeDescriptor, message);

                var invoker = _consumerInvokerFactory.CreateInvoker(consumerContext);

                invoker.InvokeAsync();

                _messageManager.UpdateAsync(consistencyMessage).Wait();

            }
            catch (Exception ex) {

                _logger.LogError("exception raised when excute method : " + ex.Message);
            }
        }
    }
}
