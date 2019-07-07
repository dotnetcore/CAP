// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    internal class PostgreSqlCollectProcessor : ICollectProcessor
    {
        private const int MaxBatch = 1000;

        private static readonly string[] Tables =
        {
            "published", "received"
        };

        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;
        private readonly PostgreSqlOptions _options;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public PostgreSqlCollectProcessor(ILogger<PostgreSqlCollectProcessor> logger,
            IOptions<PostgreSqlOptions> sqlServerOptions)
        {
            _logger = logger;
            _options = sqlServerOptions.Value;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            foreach (var table in Tables)
            {
                _logger.LogDebug($"Collecting expired data from table [{_options.Schema}].[{table}].");

                var removedCount = 0;
                do
                {
                    using (var connection = new NpgsqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync(
                            $"DELETE FROM \"{_options.Schema}\".\"{table}\" WHERE \"ExpiresAt\" < @now AND \"Id\" IN (SELECT \"Id\" FROM \"{_options.Schema}\".\"{table}\" LIMIT @count);",
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