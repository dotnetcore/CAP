using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly CancellationTokenSource _cts;

        public event EventHandler<CapMessage> MessageReceieved;

        private Task _compositeTask;
        private bool _disposed;

        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerClientFactory consumerClientFactory,
            ILoggerFactory loggerFactory,
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
            _cts = new CancellationTokenSource();
        }

        protected virtual void OnMessageReceieved(CapMessage message)
        {
            MessageReceieved?.Invoke(this, message);
        }

        public void Start()
        {
            var matchs = _selector.GetCandidatesMethods(_serviceProvider);
            var groupingMatchs = matchs.GroupBy(x => x.Value.Attribute.Group);

            foreach (var matchGroup in groupingMatchs)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
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

            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var messageStore = provider.GetRequiredService<ICapMessageStore>();

                var capMessage = new CapReceivedMessage(message)
                {
                    StatusName = StatusName.Enqueued,
                    Added = DateTime.Now
                };
                messageStore.StoreReceivedMessageAsync(capMessage).Wait();

                ConsumerExecutorDescriptor executeDescriptor = null;

                try
                {
                    executeDescriptor = _selector.GetTopicExector(message.KeyName);

                    var consumerContext = new ConsumerContext(executeDescriptor, message);

                    var invoker = _consumerInvokerFactory.CreateInvoker(consumerContext);

                    invoker.InvokeAsync();

                    messageStore.ChangeReceivedMessageStateAsync(capMessage, StatusName.Succeeded).Wait();
                }
                catch (Exception ex)
                {
                    _logger.ConsumerMethodExecutingFailed(executeDescriptor?.MethodInfo.Name, ex);
                }
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
                _compositeTask.Wait((int) TimeSpan.FromSeconds(60).TotalMilliseconds);
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