// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer;

public class SqlServerStorageInitializer : IStorageInitializer
{
    private readonly ILogger _logger;
    private readonly IOptions<SqlServerOptions> _options;
    private readonly IOptions<CapOptions> _capOptions;

    public SqlServerStorageInitializer(
        ILogger<SqlServerStorageInitializer> logger,
        IOptions<SqlServerOptions> options, IOptions<CapOptions> capOptions)
    {
        _capOptions = capOptions;
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

    public virtual string GetLockTableName()
    {
        return $"{_options.Value.Schema}.Lock";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var sql = CreateDbTablesScript(_options.Value.Schema);
        var connection = new SqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        object[] sqlParams =
        {
            new SqlParameter("@PubKey", $"publish_retry_{_capOptions.Value.Version}"),
            new SqlParameter("@RecKey", $"received_retry_{_capOptions.Value.Version}"),
            new SqlParameter("@LastLockTime", DateTime.MinValue){ SqlDbType = SqlDbType.DateTime2 },
        };
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);

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
END;
";
        if (_capOptions.Value.UseStorageLock)
            batchSql += $@"
IF OBJECT_ID(N'{GetLockTableName()}',N'U') IS NULL
BEGIN
CREATE TABLE {GetLockTableName()}(
	[Key] [nvarchar](128) NOT NULL,
    [Instance] [nvarchar](256) NOT NULL,
	[LastLockTime] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_{GetLockTableName()}] PRIMARY KEY CLUSTERED
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = ON, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] 
END;

INSERT INTO {GetLockTableName()} ([Key],[Instance],[LastLockTime]) VALUES(@PubKey,'',@LastLockTime);
INSERT INTO {GetLockTableName()} ([Key],[Instance],[LastLockTime]) VALUES(@RecKey,'',@LastLockTime);
";
        return batchSql;
    }
}