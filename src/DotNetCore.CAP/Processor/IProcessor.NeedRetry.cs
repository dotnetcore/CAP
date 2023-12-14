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
    private const int MinSuggestedValueForFallbackWindowLookbackSeconds = 30;
    private readonly ILogger<MessageNeedToRetryProcessor> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly TimeSpan _waitingInterval;
    private readonly IOptions<CapOptions> _options;
    private readonly IDataStorage _dataStorage;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _lookbackSeconds;
    private readonly string _instance;
    private Task? _failedRetryConsumeTask;

    public MessageNeedToRetryProcessor(IOptions<CapOptions> options, ILogger<MessageNeedToRetryProcessor> logger,
        IDispatcher dispatcher, IDataStorage dataStorage)
    {
        _options = options;
        _logger = logger;
        _dispatcher = dispatcher;
        _waitingInterval = TimeSpan.FromSeconds(options.Value.FailedRetryInterval);
        _lookbackSeconds = TimeSpan.FromSeconds(options.Value.FallbackWindowLookbackSeconds);
        _dataStorage = dataStorage;
        _ttl = _waitingInterval.Add(TimeSpan.FromSeconds(10));

        _instance = string.Concat(Helper.GetInstanceHostname(), "_", Util.GenerateWorkerId(1023));

        CheckSafeOptionsSet();
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var storage = context.Provider.GetRequiredService<IDataStorage>();

        _ = Task.Run(() => ProcessPublishedAsync(storage, context));

        if (_options.Value.UseStorageLock && _failedRetryConsumeTask is { IsCompleted: false })
        {
            await _dataStorage.RenewLockAsync($"received_retry_{_options.Value.Version}", _ttl, _instance, context.CancellationToken);

            await context.WaitAsync(_waitingInterval).ConfigureAwait(false);

            return;
        }

        _failedRetryConsumeTask = Task.Run(() => ProcessReceivedAsync(storage, context));

        _ = _failedRetryConsumeTask.ContinueWith(_ => { _failedRetryConsumeTask = null; });

        await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
    }

    private async Task ProcessPublishedAsync(IDataStorage connection, ProcessingContext context)
    {
        context.ThrowIfStopping();

        if (_options.Value.UseStorageLock && !await connection.AcquireLockAsync($"publish_retry_{_options.Value.Version}", _ttl, _instance, context.CancellationToken))
            return;

        var messages = await GetSafelyAsync(connection.GetPublishedMessagesOfNeedRetry, _lookbackSeconds).ConfigureAwait(false);

        foreach (var message in messages)
        {
            context.ThrowIfStopping();

            await _dispatcher.EnqueueToPublish(message).ConfigureAwait(false);
        }

        if (_options.Value.UseStorageLock)
            await connection.ReleaseLockAsync($"publish_retry_{_options.Value.Version}", _instance, context.CancellationToken);
    }

    private async Task ProcessReceivedAsync(IDataStorage connection, ProcessingContext context)
    {
        context.ThrowIfStopping();

        if (_options.Value.UseStorageLock && !await connection.AcquireLockAsync($"received_retry_{_options.Value.Version}", _ttl, _instance, context.CancellationToken))
            return;

        var messages = await GetSafelyAsync(connection.GetReceivedMessagesOfNeedRetry, _lookbackSeconds).ConfigureAwait(false);

        foreach (var message in messages)
        {
            context.ThrowIfStopping();

            await _dispatcher.EnqueueToExecute(message).ConfigureAwait(false);
        }

        if (_options.Value.UseStorageLock)
            await connection.ReleaseLockAsync($"received_retry_{_options.Value.Version}", _instance, context.CancellationToken);
    }

    private async Task<IEnumerable<T>> GetSafelyAsync<T>(Func<TimeSpan, Task<IEnumerable<T>>> getMessagesAsync, TimeSpan lookbackSeconds)
    {
        try
        {
            return await getMessagesAsync(lookbackSeconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(1, ex, "Get messages from storage failed. Retrying...");

            return Enumerable.Empty<T>();
        }
    }

    private void CheckSafeOptionsSet()
    {
        if (_lookbackSeconds < TimeSpan.FromSeconds(MinSuggestedValueForFallbackWindowLookbackSeconds))
        {
            _logger.LogWarning("The provided FallbackWindowLookbackSeconds of {currentSetFallbackWindowLookbackSeconds} is set to a value lower than {minSuggestedSeconds} seconds. This might cause unwanted unsafe behavior if the consumer takes more than the provided FallbackWindowLookbackSeconds to execute. ", _options.Value.FallbackWindowLookbackSeconds, MinSuggestedValueForFallbackWindowLookbackSeconds);
        }
    }
}