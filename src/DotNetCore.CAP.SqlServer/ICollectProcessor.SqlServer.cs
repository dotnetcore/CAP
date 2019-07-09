// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerCollectProcessor : ICollectProcessor
    {
        private const int MaxBatch = 1000;

        private static readonly string[] Tables =
        {
            "Published", "Received"
        };

        private readonly ILogger _logger;
        private readonly SqlServerOptions _options;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public SqlServerCollectProcessor(
            ILogger<SqlServerCollectProcessor> logger,
            IOptions<SqlServerOptions> sqlServerOptions)
        {
            _logger = logger;
            _options = sqlServerOptions.Value;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            foreach (var table in Tables)
            {
                _logger.LogDebug($"Collecting expired data from table [{_options.Schema}].[{table}].");

                int removedCount;
                do
                {
                    using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync($@"
DELETE TOP (@count)
FROM [{_options.Schema}].[{table}] WITH (readpast)
WHERE ExpiresAt < @now;", new {now = DateTime.Now, count = MaxBatch});
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