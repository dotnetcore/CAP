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

namespace DotNetCore.CAP
{
    internal class ConsumerHandler : IConsumerHandler
    {
        private readonly IConsumerClientFactory _consumerClientFactory;

        private readonly CancellationTokenSource _cts;
        private readonly ILogger _logger;

        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);
        private readonly MethodMatcherCache _selector;
        private readonly IServiceProvider _serviceProvider;

        private Task _compositeTask;
        private bool _disposed;

        public ConsumerHandler(
            IServiceProvider serviceProvider,
            IConsumerClientFactory consumerClientFactory,
            ILogger<ConsumerHandler> logger,
            MethodMatcherCache selector)
        {
            _selector = selector;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _consumerClientFactory = consumerClientFactory;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();

            foreach (var matchGroup in groupingMatches)
                Task.Factory.StartNew(() =>
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
                    {
                        RegisterMessageProcessor(client);

                        client.Subscribe(matchGroup.Value.Select(x => x.Attribute.Name));

                        client.Listening(_pollingDelay, _cts.Token);
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _compositeTask = Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _logger.ServerShuttingDown();
            _cts.Cancel();

            try
            {
                _compositeTask.Wait(TimeSpan.FromSeconds(10));
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                    _logger.ExpectedOperationCanceledException(innerEx);
            }
        }

        public void Pulse()
        {
            SubscribeQueuer.PulseEvent.Set();
        }

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            client.OnMessageReceived += (sender, message) =>
            {
                _logger.EnqueuingReceivedMessage(message.Name, message.Content);

                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        StoreMessage(scope, message);
                        client.Commit();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An exception occurred when storage received message. Message:'{0}'.", message);
                        client.Reject();
                    }
                }
                Pulse();
            };

            client.OnError += (sender, reason) => { _logger.MessageQueueError(reason); };
        }

        private static void StoreMessage(IServiceScope serviceScope, MessageContext messageContext)
        {
            var provider = serviceScope.ServiceProvider;
            var messageStore = provider.GetRequiredService<IStorageConnection>();
            var receivedMessage = new CapReceivedMessage(messageContext)
            {
                StatusName = StatusName.Scheduled
            };
            messageStore.StoreReceivedMessageAsync(receivedMessage).GetAwaiter().GetResult();
        }
    }
}