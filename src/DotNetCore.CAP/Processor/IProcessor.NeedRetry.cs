// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor;

public class MessageNeedToRetryProcessor : IProcessor
{
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
    private readonly ILogger<MessageNeedToRetryProcessor> _logger;
    private readonly IMessageSender _messageSender;
    private readonly ISubscribeDispatcher _subscribeDispatcher;
    private readonly IDispatcher _dispatcher;
    private readonly TimeSpan _waitingInterval;

    public MessageNeedToRetryProcessor(
        IOptions<CapOptions> options,
        ILogger<MessageNeedToRetryProcessor> logger,
        ISubscribeDispatcher subscribeDispatcher,
        IDispatcher dispatcher,
        IMessageSender messageSender)
    {
        _logger = logger;
        _subscribeDispatcher = subscribeDispatcher;
        _dispatcher = dispatcher;
        _messageSender = messageSender;
        _waitingInterval = TimeSpan.FromSeconds(options.Value.FailedRetryInterval);
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var storage = context.Provider.GetRequiredService<IDataStorage>();

        await Task.WhenAll(
            ProcessPublishedAsync(storage, context),
            ProcessReceivedAsync(storage, context),
            ProcessDelayedAsync(storage, context)).ConfigureAwait(false);

        await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
    }

    private async Task ProcessPublishedAsync(IDataStorage connection, ProcessingContext context)
    {
        context.ThrowIfStopping();

        var messages = await GetSafelyAsync(connection.GetPublishedMessagesOfNeedRetry).ConfigureAwait(false);

        foreach (var message in messages)
        {
            //the message.Origin.Value maybe JObject
            await _messageSender.SendAsync(message).ConfigureAwait(false);

            await context.WaitAsync(_delay).ConfigureAwait(false);
        }
    }

    private async Task ProcessDelayedAsync(IDataStorage connection, ProcessingContext context)
    {
        context.ThrowIfStopping();

        var messages = await GetSafelyAsync(connection.GetPublishedMessagesOfDelayed).ConfigureAwait(false);

        foreach (var message in messages)
        {
            await _dispatcher.EnqueueToScheduler(message, message.ExpiresAt!.Value).ConfigureAwait(false);

            await context.WaitAsync(_delay).ConfigureAwait(false);
        }
    }

    private async Task ProcessReceivedAsync(IDataStorage connection, ProcessingContext context)
    {
        context.ThrowIfStopping();

        var messages = await GetSafelyAsync(connection.GetReceivedMessagesOfNeedRetry).ConfigureAwait(false);

        foreach (var message in messages)
        {
            await _subscribeDispatcher.DispatchAsync(message, context.CancellationToken).ConfigureAwait(false);

            await context.WaitAsync(_delay).ConfigureAwait(false);
        }
    }

    private async Task<IEnumerable<T>> GetSafelyAsync<T>(Func<Task<IEnumerable<T>>> getMessagesAsync)
    {
        try
        {
            return await getMessagesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(1, ex, "Get messages from storage failed. Retrying...");

            return Enumerable.Empty<T>();
        }
    }
}