﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.GBase8t
{
    public class GBase8tCollectProcessor : ICollectProcessor
    {
        private const int MaxBatch = 1000;

        private static readonly string[] Tables =
        {
            "Published", "Received"
        };

        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;
        private readonly GBase8tOptions _options;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public GBase8tCollectProcessor(ILogger<GBase8tCollectProcessor> logger,
            GBase8tOptions sqlServerOptions)
        {
            _logger = logger;
            _options = sqlServerOptions;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            foreach (var table in Tables)
            {
                _logger.LogDebug($"Collecting expired data from table {_options.Schema}.{table}.");

                int removedCount;
                do
                {
                    using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync($@"
DELETE from {_options.Schema}.{table} 
where ExpiresAt in (select ExpiresAt from (select skip 0 first @count ExpiresAt from {_options.Schema}.{table} WHERE ExpiresAt < @now));", new { now = DateTime.Now, count = MaxBatch });
                    }

                    if (removedCount != 0)
                    {
                        await context.WaitAsync(_delay);
                        context.ThrowIfStopping();
                    }
                } while (removedCount != 0);
            }

            await context.WaitAsync(_waitingInterval);
        }
    }
}
