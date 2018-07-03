﻿// Copyright (c) .NET Core Community. All rights reserved.
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
            foreach (var message in messages)
            {
                if (message.Retries > _options.FailedRetryCount)
                {
                    continue;
                }

                using (var transaction = connection.CreateTransaction())
                {
                    var result = await _publishExecutor.PublishAsync(message.Name, message.Content);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("The message was sent successfully during the retry. MessageId:" + message.Id);
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

            foreach (var message in messages)
            {
                if (message.Retries > _options.FailedRetryCount)
                {
                    continue;
                }

                using (var transaction = connection.CreateTransaction())
                {
                    var result = await _subscriberExecutor.ExecuteAsync(message);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("The message was execute successfully during the retry. MessageId:" + message.Id);
                    }
                    await transaction.CommitAsync();
                }

                context.ThrowIfStopping();

                await context.WaitAsync(_delay);
            }
        }

        public DateTime GetDueTime(DateTime addedTime, int retries)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;
            return addedTime.AddSeconds(retryBehavior.RetryIn(retries));
        }
    }
}