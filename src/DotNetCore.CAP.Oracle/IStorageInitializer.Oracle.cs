// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Oracle
{
    public class OracleStorageInitializer : IStorageInitializer
    {
        private const string PUBLISHED_TABLE = "PUBLISHED";
        private const string RECEIVED_TABLE = "RECEIVED";
        private readonly ILogger _logger;
        private readonly IOptions<OracleOptions> _options;


        public OracleStorageInitializer(
            ILogger<OracleStorageInitializer> logger,
            IOptions<OracleOptions> options)
        {
            _options = options;
            _logger = logger;
        }

        public virtual string GetPublishedTableName()
        {
            return $@"""{_options.Value.Schema}"".""{PUBLISHED_TABLE}""";
        }

        public virtual string GetReceivedTableName()
        {
            return $@"""{_options.Value.Schema}"".""{RECEIVED_TABLE}""";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var sql = CreateDbTablesScript(_options.Value.Schema);
            using (var connection = new OracleConnection(_options.Value.ConnectionString))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }


        protected virtual string CreateDbTablesScript(string schema)
        {
            var batchSql = $"DECLARE " +
                $"num1 NUMBER; num2 NUMBER;" +
                $"BEGIN " +
                $"SELECT COUNT(*) INTO num1 FROM all_tables WHERE \"OWNER\" = '{_options.Value.Schema}' AND \"TABLE_NAME\"='{RECEIVED_TABLE}';" +
                $"SELECT COUNT(*) INTO num2 FROM all_tables WHERE \"OWNER\" = '{_options.Value.Schema}' AND \"TABLE_NAME\" = '{PUBLISHED_TABLE}';" +
                $"IF num1<1 THEN " +
                $"EXECUTE IMMEDIATE 'CREATE TABLE {GetReceivedTableName()}(" +
                $"\"Id\" NUMBER PRIMARY KEY NOT NULL," +
                $"\"Version\" VARCHAR2(20) NOT NULL," +
                $"\"Name\" VARCHAR2(200) NOT NULL," +
                $"\"Group\" VARCHAR2(200) NULL," +
                $"\"Content\" CLOB NULL," +
                $"\"Retries\" INT NOT NULL," +
                $"\"Added\" TIMESTAMP NOT NULL," +
                $"\"ExpiresAt\" TIMESTAMP NULL," +
                $"\"StatusName\" VARCHAR2(50) NOT NULL)';" +
                $"END IF;" +
                $"IF num2<1 THEN EXECUTE IMMEDIATE 'CREATE TABLE {GetPublishedTableName()} (" +
                $"\"Id\" NUMBER PRIMARY KEY NOT NULL," +
                $"\"Version\" VARCHAR2(20) NOT NULL," +
                $"\"Name\" VARCHAR2(200) NOT NULL," +
                $"\"Content\" CLOB NULL," +
                $"\"Retries\" INT NOT NULL," +
                $"\"Added\" TIMESTAMP NOT NULL," +
                $"\"ExpiresAt\" TIMESTAMP NULL,\"StatusName\" NVARCHAR2(50) NOT NULL)';" +
                $"END IF;" +
                $"END;";
            return batchSql;
        }
    }
}
