using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class DefaultAdditionalProcessor : IAdditionalProcessor
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly PostgreSqlOptions _options;

        private const int MaxBatch = 1000;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _waitingInterval = TimeSpan.FromHours(2);

        private static readonly string[] Tables =
        {
            "published","received"
        };

        public DefaultAdditionalProcessor(
            IServiceProvider provider,
            ILogger<DefaultAdditionalProcessor> logger,
            PostgreSqlOptions sqlServerOptions)
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
                    using (var connection = new NpgsqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync($"DELETE FROM \"{_options.Schema}\".\"{table}\" WHERE \"ExpiresAt\" < @now AND \"Id\" IN (SELECT \"Id\" FROM \"{_options.Schema}\".\"{table}\" LIMIT @count);",
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