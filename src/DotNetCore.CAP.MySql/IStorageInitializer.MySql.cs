// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DotNetCore.CAP.MySql;

public class MySqlStorageInitializer : IStorageInitializer
{
    private readonly ILogger _logger;
    private readonly IOptions<MySqlOptions> _options;

    private ServerVersion? _serverVersion;

    public MySqlStorageInitializer(
        ILogger<MySqlStorageInitializer> logger,
        IOptions<MySqlOptions> options)
    {
        _options = options;
        _logger = logger;
    }

    public virtual string GetPublishedTableName()
    {
        return $"{_options.Value.TableNamePrefix}.published";
    }

    public virtual string GetReceivedTableName()
    {
        return $"{_options.Value.TableNamePrefix}.received";
    }

    public virtual bool IsSupportSkipLocked()
    {
        if (_serverVersion == null) return false;

        switch (_serverVersion.Type)
        {
            case ServerVersion.ServerType.MySql when _serverVersion.Version.Major >= 8:
            case ServerVersion.ServerType.MariaDb when _serverVersion.Version.Major > 10:
            case ServerVersion.ServerType.MariaDb when _serverVersion.Version.Major == 10 && _serverVersion.Version.Minor >= 6:
                return true;
            default:
                return false;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var sql = CreateDbTablesScript();
        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken);
            _serverVersion = ServerVersion.Parse(connection.ServerVersion);
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }

        _logger.LogDebug("Ensuring all create database tables script are applied.");
    }


    protected virtual string CreateDbTablesScript()
    {
        var batchSql =
            $@"
CREATE TABLE IF NOT EXISTS `{GetReceivedTableName()}` (
  `Id` bigint NOT NULL,
  `Version` varchar(20) DEFAULT NULL,
  `Name` varchar(400) NOT NULL,
  `Group` varchar(200) DEFAULT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_ExpiresAt`(`ExpiresAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{GetPublishedTableName()}` (
  `Id` bigint NOT NULL,
  `Version` varchar(20) DEFAULT NULL,
  `Name` varchar(200) NOT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(40) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_ExpiresAt`(`ExpiresAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
        return batchSql;
    }
}