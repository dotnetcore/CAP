using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
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

        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);

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

                        foreach (var item in matchGroup.Value)
                        {
                            client.Subscribe(item.Attribute.Name);
                        }

                        client.Listening(_pollingDelay);
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
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

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            client.MessageReceieved += (sender, message) =>
            {
                _logger.EnqueuingReceivedMessage(message.KeyName, message.Content);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var receviedMessage = StoreMessage(scope, message);
                    client.Commit();
                   // ProcessMessage(scope, receviedMessage);
                }
            };
        }

        private CapReceivedMessage StoreMessage(IServiceScope serviceScope, MessageContext messageContext)
        {
            var provider = serviceScope.ServiceProvider;
            var messageStore = provider.GetRequiredService<IStorageConnection>();
            var receivedMessage = new CapReceivedMessage(messageContext)
            {
                StatusName = StatusName.Enqueued,
            };
            messageStore.StoreReceivedMessageAsync(receivedMessage).Wait();
            return receivedMessage;
        }

        public void Pulse()
        {
            throw new NotImplementedException();
        }

        //private void ProcessMessage(IServiceScope serviceScope, CapReceivedMessage receivedMessage)
        //{
        //    var provider = serviceScope.ServiceProvider;
        //    var messageStore = provider.GetRequiredService<IStorageConnection>();
        //    try
        //    {
        //        var executeDescriptorGroup = _selector.GetTopicExector(receivedMessage.KeyName);

        //        if (executeDescriptorGroup.ContainsKey(receivedMessage.Group))
        //        {
        //            messageStore.FetchNextReceivedMessageAsync



        //            messageStore.ChangeReceivedMessageStateAsync(receivedMessage, StatusName.Processing).Wait();

        //            // If there are multiple consumers in the same group, we will take the first
        //            var executeDescriptor = executeDescriptorGroup[receivedMessage.Group][0];
        //            var consumerContext = new ConsumerContext(executeDescriptor, receivedMessage.ToMessageContext());

        //            _consumerInvokerFactory.CreateInvoker(consumerContext).InvokeAsync();

        //            messageStore.ChangeReceivedMessageStateAsync(receivedMessage, StatusName.Succeeded).Wait();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ConsumerMethodExecutingFailed($"Group:{receivedMessage.Group}, Topic:{receivedMessage.KeyName}", ex);
        //    }
        //}


    }
}