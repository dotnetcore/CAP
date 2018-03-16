// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class NeedRetryMessageProcessor : IProcessor
    {
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IPublishExecutor _publishExecutor;
        private readonly IStateChanger _stateChanger;
        private readonly ISubscriberExecutor _subscriberExecutor;
        private readonly TimeSpan _waitingInterval;

        public NeedRetryMessageProcessor(
            IOptions<CapOptions> options,
            ILogger<NeedRetryMessageProcessor> logger,
            IStateChanger stateChanger,
            ISubscriberExecutor subscriberExecutor,
            IPublishExecutor publishExecutor)
        {
            _options = options.Value;
            _logger = logger;
            _stateChanger = stateChanger;
            _subscriberExecutor = subscriberExecutor;
            _publishExecutor = publishExecutor;
            _waitingInterval = TimeSpan.FromSeconds(_options.FailedRetryInterval);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var connection = context.Provider.GetRequiredService<IStorageConnection>();

            await Task.WhenAll(
                ProcessPublishedAsync(connection, context),
                ProcessReceivedAsync(connection, context));

            await context.WaitAsync(_waitingInterval);
        }

        private async Task ProcessPublishedAsync(IStorageConnection connection, ProcessingContext context)
        {
            var messages = await connection.GetPublishedMessagesOfNeedRetry();
            var hasException = false;

            foreach (var message in messages)
            {
                if (message.Retries > _options.FailedRetryCount)
                {
                    continue;
                }

                if (!hasException)
                {
                    try
                    {
                        _options.FailedCallback?.Invoke(MessageType.Publish, message.Name, message.Content);
                    }
                    catch (Exception ex)
                    {
                        hasException = true;
                        _logger.LogWarning("Failed call-back method raised an exception:" + ex.Message);
                    }
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
            var messages = await connection.GetReceivedMessagesOfNeedRetry();
            var hasException = false;

            foreach (var message in messages)
            {
                if (message.Retries > _options.FailedRetryCount)
                {
                    continue;
                }

                if (!hasException)
                {
                    try
                    {
                        _options.FailedCallback?.Invoke(MessageType.Subscribe, message.Name, message.Content);
                    }
                    catch (Exception ex)
                    {
                        hasException = true;
                        _logger.LogWarning("Failed call-back method raised an exception:" + ex.Message);
                    }
                }

                await _subscriberExecutor.ExecuteAsync(message);

                context.ThrowIfStopping();

                await context.WaitAsync(_delay);
            }
        }
    }
}