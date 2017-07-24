using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorage : IStorage
    {
        private readonly MySqlOptions _options;
        private readonly ILogger _logger;

        public MySqlStorage(ILogger<MySqlStorage> logger, MySqlOptions options)
        {
            _options = options;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
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
CREATE TABLE IF NOT EXISTS `{prefix}.queue` (
  `MessageId` int(11) NOT NULL,
  `MessageType` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `{prefix}.received` (
  `Id` int(127) NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) NOT NULL,
  `Group` varchar(200) DEFAULT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime(6) NOT NULL,
  `ExpiresAt` datetime(6) DEFAULT NULL,
  `StatusName` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `{prefix}.published` (
  `Id` int(127) NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) NOT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime(6) NOT NULL,
  `ExpiresAt` datetime(6) DEFAULT NULL,
  `StatusName` varchar(40) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            return batchSql;
        }
    }
}