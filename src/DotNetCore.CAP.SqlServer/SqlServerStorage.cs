using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorage : IStorage
    {
        private readonly SqlServerOptions _options;
        private readonly ILogger _logger;

        public SqlServerStorage(ILogger<SqlServerStorage> logger, SqlServerOptions options)
        {
            _options = options;
            _logger = logger;
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
    }
}