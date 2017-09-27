using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlStorage : IStorage
    {
        private readonly PostgreSqlOptions _options;
        private readonly ILogger _logger;

        public PostgreSqlStorage(ILogger<PostgreSqlStorage> logger, PostgreSqlOptions options)
        {
            _options = options;
            _logger = logger;
        }

        public IStorageConnection GetConnection()
        {
            throw new System.NotImplementedException();
        }

        public IMonitoringApi GetMonitoringApi()
        {
            throw new System.NotImplementedException();
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var sql = CreateDbTablesScript(_options.Schema);

            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }
            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected virtual string CreateDbTablesScript(string schema)
        {
            var batchSql = $@"
CREATE SCHEMA IF NOT EXISTS ""{schema}"";

CREATE TABLE IF NOT EXISTS ""{schema}"".""queue""(
	""MessageId"" int NOT NULL ,
	""MessageType"" int NOT NULL
);

CREATE TABLE IF NOT EXISTS ""{schema}"".""received""(
	""Id"" SERIAL PRIMARY KEY NOT NULL,
	""Name"" VARCHAR(200) NOT NULL,
	""Group"" VARCHAR(200) NULL,
	""Content"" TEXT NULL,
	""Retries"" INT NOT NULL,
	""Added"" TIMESTAMP NOT NULL,
    ""ExpiresAt"" TIMESTAMP NULL,
	""StatusName"" VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS ""{schema}"".""published""(
	""Id"" SERIAL PRIMARY KEY NOT NULL,
	""Name"" VARCHAR(200) NOT NULL,
	""Content"" TEXT NULL,
	""Retries"" INT NOT NULL,
	""Added"" TIMESTAMP NOT NULL,
    ""ExpiresAt"" TIMESTAMP NULL,
	""StatusName"" VARCHAR(50) NOT NULL
);";
            return batchSql;
        }
    }
}