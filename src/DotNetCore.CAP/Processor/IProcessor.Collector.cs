// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class CollectorProcessor : IProcessor
    {
        private readonly ILogger _logger;
        private readonly IStorageInitializer _initializer;
        private readonly IDataStorage _storage;

        private const int ItemBatch = 1000;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

        public CollectorProcessor(
            ILogger<CollectorProcessor> logger,
            IStorageInitializer initializer,
            IDataStorage storage)
        {
            _logger = logger;
            _initializer = initializer;
            _storage = storage;
        } 

        public async Task ProcessAsync(ProcessingContext context)
        {
            var tables = new[]
            {
                _initializer.GetPublishedTableName(),
                _initializer.GetReceivedTableName()
            };

            foreach (var table in tables)
            {
                _logger.LogDebug($"Collecting expired data from table: {table}");

                int deletedCount;
                var time = DateTime.Now;
                do
                {
                    deletedCount = await _storage.DeleteExpiresAsync(table, time, ItemBatch, context.CancellationToken);

                    if (deletedCount != 0)
                    {
                        await context.WaitAsync(_delay);
                        context.ThrowIfStopping();
                    }
                } while (deletedCount != 0);
            }

            await context.WaitAsync(_waitingInterval);
        }
    }
}