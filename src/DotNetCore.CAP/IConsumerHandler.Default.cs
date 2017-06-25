using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    public class ConsumerHandler : IConsumerHandler, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConsumerInvokerFactory _consumerInvokerFactory;
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private readonly MethodMatcherCache _selector;
        private readonly CapOptions _options;
        private readonly ICapMessageStore _messageStore;
        private readonly CancellationTokenSource _cts;

        public event EventHandler<CapMessage> MessageReceieved;

        private CapStartContext _context;
        private Task _compositeTask;
        private bool _disposed;

        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerClientFactory consumerClientFactory,
            ILoggerFactory loggerFactory,
            ICapMessageStore messageStore,
            MethodMatcherCache selector,
            IOptions<CapOptions> options)
        {
            _selector = selector;
            _logger = loggerFactory.CreateLogger<ConsumerHandler>();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _consumerInvokerFactory = consumerInvokerFactory;
            _consumerClientFactory = consumerClientFactory;
            _options = options.Value;
            _messageStore = messageStore;
            _cts = new CancellationTokenSource();
        }

        protected virtual void OnMessageReceieved(CapMessage message)
        {
            MessageReceieved?.Invoke(this, message);
        }

        public void Start()
        {
            _context = new CapStartContext(_serviceProvider, _cts.Token);

            var matchs = _selector.GetCandidatesMethods(_context);

            var groupingMatchs = matchs.GroupBy(x => x.Value.Attribute.GroupOrExchange);

            foreach (var matchGroup in groupingMatchs)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key, _options.BrokerUrlList))
                    {
                        client.MessageReceieved += OnMessageReceieved;

                        foreach (var item in matchGroup)
                        {
                            client.Subscribe(item.Key);
                        }

                        client.Listening(TimeSpan.FromSeconds(1));
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }
            _compositeTask = Task.CompletedTask;
        }

        public virtual void OnMessageReceieved(object sender, MessageBase message)
        {
            _logger.EnqueuingReceivedMessage(message.KeyName, message.Content);

            var capMessage = new CapReceivedMessage(message)
            {
                StateName = StateName.Enqueued,
                Added = DateTime.Now
            };
            _messageStore.StoreReceivedMessageAsync(capMessage).Wait();

            ConsumerExecutorDescriptor executeDescriptor = null;

            try
            {
                executeDescriptor = _selector.GetTopicExector(message.KeyName);

                var consumerContext = new ConsumerContext(executeDescriptor, message);

                var invoker = _consumerInvokerFactory.CreateInvoker(consumerContext);

                invoker.InvokeAsync();

                _messageStore.UpdateReceivedMessageAsync(capMessage).Wait();
            }
            catch (Exception ex)
            {
                _logger.ConsumerMethodExecutingFailed(executeDescriptor.MethodInfo.Name, ex);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _logger.ServerShuttingDown();
            _cts.Cancel();

            try
            {
                _compositeTask.Wait((int)TimeSpan.FromSeconds(60).TotalMilliseconds);
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                {
                    _logger.ExpectedOperationCanceledException(innerEx);
                }
            }
        }
    }
}