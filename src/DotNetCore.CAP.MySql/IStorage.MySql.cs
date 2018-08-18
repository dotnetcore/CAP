// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorage : IStorage
    {
        private readonly CapOptions _capOptions;
        private readonly IDbConnection _existingConnection = null;
        private readonly ILogger _logger;
        private readonly MySqlOptions _options;

        public MySqlStorage(ILogger<MySqlStorage> logger,
            MySqlOptions options,
            CapOptions capOptions)
        {
            _options = options;
            _capOptions = capOptions;
            _logger = logger;
        }

        public IStorageConnection GetConnection()
        {
            return new MySqlStorageConnection(_options, _capOptions);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new MySqlMonitoringApi(this, _options);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var sql = CreateDbTablesScript(_options.TableNamePrefix);
            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected virtual string CreateDbTablesScript(string prefix)
        {
            var batchSql =
                $@"
DROP TABLE IF EXISTS `{prefix}.queue`;

CREATE TABLE IF NOT EXISTS `{prefix}.received` (
  `Id` int(127) NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) NOT NULL,
  `Group` varchar(200) DEFAULT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `{prefix}.published` (
  `Id` int(127) NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) NOT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(40) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE `{prefix}.published` MODIFY Id BIGINT NOT NULL;
ALTER TABLE `{prefix}.received` MODIFY Id BIGINT NOT NULL;
";
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
            var connection = _existingConnection ?? new MySqlConnection(_options.ConnectionString);

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