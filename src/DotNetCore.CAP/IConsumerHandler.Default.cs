using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
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
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _cts;
        private readonly MethodMatcherCache _selector;
        private readonly CapOptions _options;

        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);

        private Task _compositeTask;
        private bool _disposed;

        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerClientFactory consumerClientFactory,
            ILogger<ConsumerHandler> logger,
            MethodMatcherCache selector,
            IOptions<CapOptions> options)
        {
            _selector = selector;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _consumerInvokerFactory = consumerInvokerFactory;
            _consumerClientFactory = consumerClientFactory;
            _options = options.Value;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            var groupingMatchs = _selector.GetCandidatesMethodsOfGroupNameGrouped(_serviceProvider);

            foreach (var matchGroup in groupingMatchs)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
                    {
                        RegisterMessageProcessor(client);

                        client.Subscribe(matchGroup.Value.Select(x => x.Attribute.Name));

                        client.Listening(_pollingDelay, _cts.Token);
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            _compositeTask = Task.CompletedTask;
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
                _compositeTask.Wait(TimeSpan.FromSeconds(60));
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

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            client.OnMessageReceieved += (sender, message) =>
            {
                _logger.EnqueuingReceivedMessage(message.Name, message.Content);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var receviedMessage = StoreMessage(scope, message);
                    client.Commit();
                }
                Pulse();
            };

            client.OnError += (sender, reason) =>
            {
                _logger.LogError(reason);
            };
        }

        private CapReceivedMessage StoreMessage(IServiceScope serviceScope, MessageContext messageContext)
        {
            var provider = serviceScope.ServiceProvider;
            var messageStore = provider.GetRequiredService<IStorageConnection>();
            var receivedMessage = new CapReceivedMessage(messageContext)
            {
                StatusName = StatusName.Scheduled,
            };
            messageStore.StoreReceivedMessageAsync(receivedMessage).GetAwaiter().GetResult();
            return receivedMessage;
        }

        public void Pulse()
        {
            SubscribeQueuer.PulseEvent.Set();
        }
    }
}