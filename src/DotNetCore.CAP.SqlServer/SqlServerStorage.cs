using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorage : IStorage
    {
        private readonly IDbConnection _existingConnection = null;
        private readonly ILogger _logger;
        private readonly CapOptions _capOptions;
        private readonly SqlServerOptions _options;

        public SqlServerStorage(ILogger<SqlServerStorage> logger,
            CapOptions capOptions,
            SqlServerOptions options)
        {
            _options = options;
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

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

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
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schema}')
BEGIN
	EXEC('CREATE SCHEMA {schema}')
END;

IF OBJECT_ID(N'[{schema}].[Queue]',N'U') IS NULL
BEGIN
	CREATE TABLE [{schema}].[Queue](
		[MessageId] [int] NOT NULL,
		[MessageType] [tinyint] NOT NULL
	) ON [PRIMARY]
END;

IF OBJECT_ID(N'[{schema}].[Received]',N'U') IS NULL
BEGIN
CREATE TABLE [{schema}].[Received](
	[Id] [int] IDENTITY(1,1) NOT NULL,
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
	[Id] [int] IDENTITY(1,1) NOT NULL,
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
            var connection = _existingConnection ?? new SqlConnection(_options.ConnectionString);

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
    }
}