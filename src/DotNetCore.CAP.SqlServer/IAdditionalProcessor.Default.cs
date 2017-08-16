using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.SqlServer
{
    public class DefaultAdditionalProcessor : IAdditionalProcessor
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly SqlServerOptions _options;

        private const int MaxBatch = 1000;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        private static readonly string[] Tables =
        {
            "Published","Received"
        };

        public DefaultAdditionalProcessor(
            IServiceProvider provider,
            ILogger<DefaultAdditionalProcessor> logger,
            SqlServerOptions sqlServerOptions)
        {
            _logger = logger;
            _provider = provider;
            _options = sqlServerOptions;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug("Collecting expired entities.");

            foreach (var table in Tables)
            {
                var removedCount = 0;
                do
                {
                    using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync($@"
DELETE TOP (@count)
FROM [{_options.Schema}].[{table}] WITH (readpast)
WHERE ExpiresAt < @now;", new { now = DateTime.Now, count = MaxBatch });
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