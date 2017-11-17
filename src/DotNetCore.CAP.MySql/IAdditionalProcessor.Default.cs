using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    internal class DefaultAdditionalProcessor : IAdditionalProcessor
    {
        private const int MaxBatch = 1000;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;
        private readonly MySqlOptions _options;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public DefaultAdditionalProcessor(ILogger<DefaultAdditionalProcessor> logger,
            MySqlOptions mysqlOptions)
        {
            _logger = logger;
            _options = mysqlOptions;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug("Collecting expired entities.");

            var tables = new[]
            {
                $"{_options.TableNamePrefix}.published",
                $"{_options.TableNamePrefix}.received"
            };

            foreach (var table in tables)
            {
                int removedCount;
                do
                {
                    using (var connection = new MySqlConnection(_options.ConnectionString))
                    {
                        removedCount = await connection.ExecuteAsync(
                            $@"DELETE FROM `{table}` WHERE ExpiresAt < @now limit @count;",
                            new {now = DateTime.Now, count = MaxBatch});
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