// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlStorageInitializer : IStorageInitializer
    {
        private readonly ILogger _logger;
        private readonly IOptions<PostgreSqlOptions> _options;
        private readonly IOptions<CapOptions> _capOptions;

        public PostgreSqlStorageInitializer(
            ILogger<PostgreSqlStorageInitializer> logger,
            IOptions<PostgreSqlOptions> options, IOptions<CapOptions> capOptions)
        {
            _capOptions = capOptions;
            _options = options;
            _logger = logger;
        }

        public virtual string GetPublishedTableName()
        {
            return $"\"{_options.Value.Schema}\".\"published\"";
        }

        public virtual string GetReceivedTableName()
        {
            return $"\"{_options.Value.Schema}\".\"received\"";
        }

        public virtual string GetLockTableName()
        {
            return $"\"{_options.Value.Schema}\".\"lock\"";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var sql = CreateDbTablesScript(_options.Value.Schema);
            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new NpgsqlParameter("@PubKey", $"publish_retry_{_capOptions.Value.Version}"),
                new NpgsqlParameter("@RecKey", $"received_retry_{_capOptions.Value.Version}"),
                new NpgsqlParameter("@LastLockTime", DateTime.MinValue),
            };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected virtual string CreateDbTablesScript(string schema)
        {
            var batchSql = $@"
CREATE SCHEMA IF NOT EXISTS ""{schema}"";

CREATE TABLE IF NOT EXISTS {GetReceivedTableName()}(
	""Id"" BIGINT PRIMARY KEY NOT NULL,
    ""Version"" VARCHAR(20) NOT NULL,
	""Name"" VARCHAR(200) NOT NULL,
	""Group"" VARCHAR(200) NULL,
	""Content"" TEXT NULL,
	""Retries"" INT NOT NULL,
	""Added"" TIMESTAMP NOT NULL,
    ""ExpiresAt"" TIMESTAMP NULL,
	""StatusName"" VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS {GetPublishedTableName()}(
	""Id"" BIGINT PRIMARY KEY NOT NULL,
    ""Version"" VARCHAR(20) NOT NULL,
	""Name"" VARCHAR(200) NOT NULL,
	""Content"" TEXT NULL,
	""Retries"" INT NOT NULL,
	""Added"" TIMESTAMP NOT NULL,
    ""ExpiresAt"" TIMESTAMP NULL,
	""StatusName"" VARCHAR(50) NOT NULL
);
";
            if (_capOptions.Value.UseStorageLock)
                batchSql += $@"
CREATE TABLE IF NOT EXISTS {GetLockTableName()}(
	""Key"" VARCHAR(128) PRIMARY KEY NOT NULL,
    ""Instance"" VARCHAR(256),
	""LastLockTime"" TIMESTAMP NOT NULL
);

INSERT INTO {GetLockTableName()} (""Key"",""Instance"",""LastLockTime"") VALUES(@PubKey,'',@LastLockTime) ON CONFLICT DO NOTHING;
INSERT INTO {GetLockTableName()} (""Key"",""Instance"",""LastLockTime"") VALUES(@RecKey,'',@LastLockTime) ON CONFLICT DO NOTHING;";

            return batchSql;
        }
    }
}