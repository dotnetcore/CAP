// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.SqlServer.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorage : IStorage
    {
        private readonly ILogger _logger;
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IOptions<SqlServerOptions> _options;
        private readonly IDbConnection _existingConnection = null;
        private readonly DiagnosticProcessorObserver _diagnosticProcessorObserver;

        public SqlServerStorage(
            ILogger<SqlServerStorage> logger,
            IOptions<CapOptions> capOptions,
            IOptions<SqlServerOptions> options,
            DiagnosticProcessorObserver diagnosticProcessorObserver)
        {
            _options = options;
            _diagnosticProcessorObserver = diagnosticProcessorObserver;
            _logger = logger;
            _capOptions = capOptions;
        }

        public IStorageConnection GetConnection()
        {
            return new SqlServerStorageConnection(_options, _capOptions);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(this, _options);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var sql = CreateDbTablesScript(_options.Value.Schema);

            using (var connection = new SqlConnection(_options.Value.ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }

            _logger.LogDebug("Ensuring all create database tables script are applied.");

            DiagnosticListener.AllListeners.Subscribe(_diagnosticProcessorObserver);
        }

        protected virtual string CreateDbTablesScript(string schema)
        {
            var batchSql =
                $@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schema}')
BEGIN
	EXEC('CREATE SCHEMA [{schema}]')
END;

IF OBJECT_ID(N'[{schema}].[Received]',N'U') IS NULL
BEGIN
CREATE TABLE [{schema}].[Received](
	[Id] [bigint] NOT NULL,
    [Version] [nvarchar](20) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Group] [nvarchar](200) NULL,
	[Content] [nvarchar](max) NULL,
	[Retries] [int] NOT NULL,
	[Added] [datetime2](7) NOT NULL,
    [ExpiresAt] [datetime2](7) NULL,
	[StatusName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_{schema}.Received] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;

IF OBJECT_ID(N'[{schema}].[Published]',N'U') IS NULL
BEGIN
CREATE TABLE [{schema}].[Published](
	[Id] [bigint] NOT NULL,
    [Version] [nvarchar](20) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Content] [nvarchar](max) NULL,
	[Retries] [int] NOT NULL,
	[Added] [datetime2](7) NOT NULL,
    [ExpiresAt] [datetime2](7) NULL,
	[StatusName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_{schema}.Published] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;";
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
            var connection = _existingConnection ?? new SqlConnection(_options.Value.ConnectionString);

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