using System;
using System.Data;
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
        private readonly IDbConnection _existingConnection = null;
        private readonly ILogger _logger;
        private readonly CapOptions _capOptions;
        private readonly PostgreSqlOptions _options;

        public PostgreSqlStorage(ILogger<PostgreSqlStorage> logger,
            CapOptions capOptions,
            PostgreSqlOptions options)
        {
            _options = options;
            _logger = logger;
            _capOptions = capOptions;
        }

        public IStorageConnection GetConnection()
        {
            return new PostgreSqlStorageConnection(_options, _capOptions);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new PostgreSqlMonitoringApi(this, _options);
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

        internal T UseConnection<T>(Func<IDbConnection, T> func)
        {
            IDbConnection connection = null;

            try
            {
                connection = CreateAndOpenConnection();
                return func(connection);
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }

        internal IDbConnection CreateAndOpenConnection()
        {
            var connection = _existingConnection ?? new NpgsqlConnection(_options.ConnectionString);

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            return connection;
        }

        internal bool IsExistingConnection(IDbConnection connection)
        {
            return connection != null && ReferenceEquals(connection, _existingConnection);
        }

        internal void ReleaseConnection(IDbConnection connection)
        {
            if (connection != null && !IsExistingConnection(connection))
                connection.Dispose();
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