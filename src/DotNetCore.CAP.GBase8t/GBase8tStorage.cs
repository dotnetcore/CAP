// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;


namespace DotNetCore.CAP.GBase8t
{
    public class GBase8tStorage : IStorage
    {
        private readonly CapOptions _capOptions;
        private readonly IDbConnection _existingConnection = null;
        private readonly ILogger _logger;
        private readonly GBase8tOptions _options;

        public GBase8tStorage(ILogger<GBase8tStorage> logger,
            CapOptions capOptions,
            GBase8tOptions options)
        {
            _options = options;
            _logger = logger;
            _capOptions = capOptions;
        }

        public IStorageConnection GetConnection()
        {
            return new GBase8tStorageConnection(_options, _capOptions);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(this, _options);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var sql = CreateDbTablesScript(_options.Schema);

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected virtual string CreateDbTablesScript(string schema)
        {
            var batchSql =
                 $@"
CREATE schema AUTHORIZATION {schema}
CREATE TABLE if not exists {schema}.Received(
	Id serial  NOT NULL,
	Name VARCHAR(200) NOT NULL,
	Group VARCHAR(200) NULL,
	Content LVARCHAR(2000) NULL,
	Retries INT8 NOT NULL,
	Added DATETIME YEAR TO FRACTION(5) NOT NULL,
    ExpiresAt DATETIME YEAR TO FRACTION(5) NULL,
	StatusName VARCHAR(50) NOT NULL,
	PRIMARY KEY (Id));

CREATE schema AUTHORIZATION {schema}
CREATE TABLE if not exists {schema}.Published(
	Id serial NOT NULL,
	Name VARCHAR(200) NOT NULL,
	Content LVARCHAR(2000) NULL,
	Retries INT8 NOT NULL,
	Added DATETIME YEAR TO FRACTION(5) NOT NULL,
    ExpiresAt DATETIME YEAR TO FRACTION(5) NULL,
	StatusName VARCHAR(50) NOT NULL,
	PRIMARY KEY (Id));";
            return batchSql;
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
            var connection = _existingConnection ?? new SqlConnection(_options.ConnectionString);

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            return connection;
        }

        internal bool IsExistingConnection(IDbConnection connection)
        {
            return connection != null && ReferenceEquals(connection, _existingConnection);
        }

        internal void ReleaseConnection(IDbConnection connection)
        {
            if (connection != null && !IsExistingConnection(connection))
            {
                connection.Dispose();
            }
        }
    }
}
