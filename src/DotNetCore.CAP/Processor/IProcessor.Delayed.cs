// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor;

public class MessageDelayedProcessor : IProcessor
{
    private readonly ILogger<MessageDelayedProcessor> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly TimeSpan _waitingInterval;

    public MessageDelayedProcessor(ILogger<MessageDelayedProcessor> logger, IDispatcher dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _waitingInterval = TimeSpan.FromSeconds(60);
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var storage = context.Provider.GetRequiredService<IDataStorage>();

        await ProcessDelayedAsync(storage, context).ConfigureAwait(false);

        await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
    }

    private async Task ProcessDelayedAsync(IDataStorage connection, ProcessingContext context)
    {
        try
        {
            async Task ScheduleTask(object transaction, IEnumerable<MediumMessage> messages)
            {
                foreach (var message in messages)
                {
                    await _dispatcher.EnqueueToScheduler(message, message.ExpiresAt!.Value, transaction).ConfigureAwait(false);
                }
            }

            await connection.ScheduleMessagesOfDelayedAsync(ScheduleTask, context.CancellationToken).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            _logger.LogWarning(ex, "Get delayed messages from storage failed. Retrying...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schedule delayed message failed!");
        }
    }
}