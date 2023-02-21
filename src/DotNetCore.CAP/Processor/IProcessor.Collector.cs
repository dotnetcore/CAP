// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor;

public class CollectorProcessor : IProcessor
{
    private const int ItemBatch = 1000;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly string[] _tableNames;
    private readonly TimeSpan _waitingInterval;

    public CollectorProcessor(ILogger<CollectorProcessor> logger, IOptions<CapOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _waitingInterval = TimeSpan.FromSeconds(options.Value.CollectorCleaningInterval);

        var initializer = _serviceProvider.GetRequiredService<IStorageInitializer>();

        _tableNames = new[] { initializer.GetPublishedTableName(), initializer.GetReceivedTableName() };
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        foreach (var table in _tableNames)
        {
            _logger.LogDebug($"Collecting expired data from table: {table}");

            int deletedCount;
            var time = DateTime.Now;
            do
            {
                deletedCount = await _serviceProvider.GetRequiredService<IDataStorage>()
                    .DeleteExpiresAsync(table, time, ItemBatch, context.CancellationToken).ConfigureAwait(false);

                if (deletedCount != 0)
                {
                    await context.WaitAsync(_delay).ConfigureAwait(false);
                    context.ThrowIfStopping();
                }
            } while (deletedCount != 0);
        }

        await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
    }
}