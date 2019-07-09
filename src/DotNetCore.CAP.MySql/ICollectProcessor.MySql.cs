// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    internal class MySqlCollectProcessor : ICollectProcessor
    {
        private const int MaxBatch = 1000;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;
        private readonly MySqlOptions _options;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public MySqlCollectProcessor(ILogger<MySqlCollectProcessor> logger, IOptions<MySqlOptions> mysqlOptions)
        {
            _logger = logger;
            _options = mysqlOptions.Value;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            var tables = new[]
            {
                $"{_options.TableNamePrefix}.published",
                $"{_options.TableNamePrefix}.received"
            };

            foreach (var table in tables)
            {
                _logger.LogDebug($"Collecting expired data from table [{table}].");

                int removedCount;
                do
                {
                    using (var connection = new MySqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync(
                            $@"DELETE FROM `{table}` WHERE ExpiresAt < @now limit @count;",
                            new { now = DateTime.Now, count = MaxBatch });
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