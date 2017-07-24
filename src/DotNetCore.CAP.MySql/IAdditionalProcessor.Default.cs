using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class DefaultAdditionalProcessor : IAdditionalProcessor
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly MySqlOptions _options;

        private const int MaxBatch = 1000;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _waitingInterval = TimeSpan.FromHours(2);

        public DefaultAdditionalProcessor(
            IServiceProvider provider,
            ILogger<DefaultAdditionalProcessor> logger,
            MySqlOptions sqlServerOptions)
        {
            _logger = logger;
            _provider = provider;
            _options = sqlServerOptions;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug("Collecting expired entities.");

            var tables = new string[]{
                $"{_options.TableNamePrefix}.published",
                $"{_options.TableNamePrefix}.received"
            };

            foreach (var table in tables)
            {
                var removedCount = 0;
                do
                {
                    using (var connection = new MySqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync($@"DELETE FROM `{table}` WHERE ExpiresAt < @now limit @count;",
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