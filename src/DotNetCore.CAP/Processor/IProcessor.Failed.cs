using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class FailedProcessor : IProcessor
    {
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IServiceProvider _provider;
        private readonly IStateChanger _stateChanger;
        private readonly ISubscriberExecutor _subscriberExecutor;
        private readonly IPublishExecutor _publishExecutor;
        private readonly TimeSpan _waitingInterval;

        public FailedProcessor(
            IOptions<CapOptions> options,
            ILogger<FailedProcessor> logger,
            IServiceProvider provider,
            IStateChanger stateChanger,
            ISubscriberExecutor subscriberExecutor,
            IPublishExecutor publishExecutor)
        {
            _options = options.Value;
            _logger = logger;
            _provider = provider;
            _stateChanger = stateChanger;
            _subscriberExecutor = subscriberExecutor;
            _publishExecutor = publishExecutor;
            _waitingInterval = TimeSpan.FromSeconds(_options.FailedRetryInterval);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            using (var scope = _provider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var connection = provider.GetRequiredService<IStorageConnection>();

                await Task.WhenAll(
                    ProcessPublishedAsync(connection, context),
                    ProcessReceivedAsync(connection, context));

                await context.WaitAsync(_waitingInterval);
            }
        }

        private async Task ProcessPublishedAsync(IStorageConnection connection, ProcessingContext context)
        {
            var messages = await connection.GetFailedPublishedMessages();
            var hasException = false;

            foreach (var message in messages)
            {
                if (message.Retries > _options.FailedRetryCount)
                    continue;

                if (!hasException)
                    try
                    {
                        _options.FailedCallback?.Invoke(MessageType.Publish, message.Name, message.Content);
                    }
                    catch (Exception ex)
                    {
                        hasException = true;
                        _logger.LogWarning("Failed call-back method raised an exception:" + ex.Message);
                    }

                using (var transaction = connection.CreateTransaction())
                {
                    try
                    {
                        await _publishExecutor.PublishAsync(message.Name, message.Content);

                        _stateChanger.ChangeState(message, new SucceededState(), transaction);
                    }
                    catch (Exception e)
                    {
                        message.Content = Helper.AddExceptionProperty(message.Content, e);
                        message.Retries++;
                        transaction.UpdateMessage(message);
                    }
                    await transaction.CommitAsync();
                }

                context.ThrowIfStopping();

                await context.WaitAsync(_delay);
            }
        }

        private async Task ProcessReceivedAsync(IStorageConnection connection, ProcessingContext context)
        {
            var messages = await connection.GetFailedReceivedMessages();
            var hasException = false;

            foreach (var message in messages)
            {
                if (message.Retries > _options.FailedRetryCount)
                    continue;

                if (!hasException)
                    try
                    {
                        _options.FailedCallback?.Invoke(MessageType.Subscribe, message.Name, message.Content);
                    }
                    catch (Exception ex)
                    {
                        hasException = true;
                        _logger.LogWarning("Failed call-back method raised an exception:" + ex.Message);
                    }

                using (var transaction = connection.CreateTransaction())
                {
                    var ret = await _subscriberExecutor.ExecuteAsync(message);
                    if (ret.Succeeded)
                    {
                        _stateChanger.ChangeState(message, new SucceededState(), transaction);
                    }
                    else
                    {
                        message.Retries++;
                        message.Content = Helper.AddExceptionProperty(message.Content, ret.Exception);
                        transaction.UpdateMessage(message);
                    }
                    await transaction.CommitAsync();
                }

                context.ThrowIfStopping();

                await context.WaitAsync(_delay);
            }
        }
    }
}