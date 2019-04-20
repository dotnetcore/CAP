// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Processor
{
    public class NeedRetryMessageProcessor : IProcessor
    {
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger<NeedRetryMessageProcessor> _logger;
        private readonly IPublishMessageSender _publishMessageSender;
        private readonly ISubscriberExecutor _subscriberExecutor;
        private readonly TimeSpan _waitingInterval;

        public NeedRetryMessageProcessor(
            CapOptions options,
            ILogger<NeedRetryMessageProcessor> logger,
            ISubscriberExecutor subscriberExecutor,
            IPublishMessageSender publishMessageSender)
        {
            _logger = logger;
            _subscriberExecutor = subscriberExecutor;
            _publishMessageSender = publishMessageSender;
            _waitingInterval = TimeSpan.FromSeconds(options.FailedRetryInterval);
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
            var messages = await GetSafelyAsync(() => connection.GetPublishedMessagesOfNeedRetry());

            foreach (var message in messages)
            {
                await _publishMessageSender.SendAsync(message);

                context.ThrowIfStopping();

                await context.WaitAsync(_delay);
            }
        }

        private async Task ProcessReceivedAsync(IStorageConnection connection, ProcessingContext context)
        {
            var messages = await GetSafelyAsync(() => connection.GetReceivedMessagesOfNeedRetry());

            foreach (var message in messages)
            {
                await _subscriberExecutor.ExecuteAsync(message);

                context.ThrowIfStopping();

                await context.WaitAsync(_delay);
            }
        } 

        private async Task<IEnumerable<T>> GetSafelyAsync<T>(Func<Task<IEnumerable<T>>> getMessagesAsync)
        {
            try
            {
                return await getMessagesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(1, ex, "Get messages of type '{messageType}' failed. Retrying...", typeof(T).Name);

                return Enumerable.Empty<T>();
            }
        }
    }
}