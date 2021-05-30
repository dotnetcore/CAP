// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorageInitializer : IStorageInitializer
    {
        private readonly ILogger _logger;
        private readonly IOptions<SqlServerOptions> _options;

        public SqlServerStorageInitializer(
            ILogger<SqlServerStorageInitializer> logger,
            IOptions<SqlServerOptions> options)
        {
            _options = options;
            _logger = logger;
        }

        public virtual string GetPublishedTableName()
        {
            return $"{_options.Value.Schema}.Published";
        }

        public virtual string GetReceivedTableName()
        {
            return $"{_options.Value.Schema}.Received";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var sql = CreateDbTablesScript(_options.Value.Schema);
            await using (var connection = new SqlConnection(_options.Value.ConnectionString))
                connection.ExecuteNonQuery(sql);

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected virtual string CreateDbTablesScript(string schema)
        {
            var batchSql =
                $@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schema}')
BEGIN
	EXEC('CREATE SCHEMA [{schema}]')
END;

IF OBJECT_ID(N'{GetReceivedTableName()}',N'U') IS NULL
BEGIN
CREATE TABLE {GetReceivedTableName()}(
	[Id] [bigint] NOT NULL,
    [Version] [nvarchar](20) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Group] [nvarchar](200) NULL,
	[Content] [nvarchar](max) NULL,
	[Retries] [int] NOT NULL,
	[Added] [datetime2](7) NOT NULL,
    [ExpiresAt] [datetime2](7) NULL,
	[StatusName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_{GetReceivedTableName()}] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;

IF OBJECT_ID(N'{GetPublishedTableName()}',N'U') IS NULL
BEGIN
CREATE TABLE {GetPublishedTableName()}(
	[Id] [bigint] NOT NULL,
    [Version] [nvarchar](20) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Content] [nvarchar](max) NULL,
	[Retries] [int] NOT NULL,
	[Added] [datetime2](7) NOT NULL,
    [ExpiresAt] [datetime2](7) NULL,
	[StatusName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_{GetPublishedTableName()}] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;";
            return batchSql;
        }
    }
}
