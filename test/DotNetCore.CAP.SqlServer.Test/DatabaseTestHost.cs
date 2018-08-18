using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotNetCore.CAP.SqlServer.Test
{
    public abstract class DatabaseTestHost:IDisposable
    {
        protected ILogger<SqlServerStorage> Logger;
        protected CapOptions CapOptions;
        protected SqlServerOptions SqlSeverOptions;

        public bool SqlObjectInstalled;

        protected DatabaseTestHost()
        {
            Logger = new Mock<ILogger<SqlServerStorage>>().Object;
            CapOptions = new Mock<CapOptions>().Object;
            SqlSeverOptions = new Mock<SqlServerOptions>()
                .SetupProperty(x => x.ConnectionString, ConnectionUtil.GetConnectionString())
                .Object;

            InitializeDatabase();
        }

        public void Dispose()
        {
            DeleteAllData();
        }

        private void InitializeDatabase()
        {
            var masterConn = ConnectionUtil.GetMasterConnectionString();
            var databaseName = ConnectionUtil.GetDatabaseName();
            using (var connection = ConnectionUtil.CreateConnection(masterConn))
            {
                connection.Execute($@"
IF NOT EXISTS (SELECT * FROM sysdatabases WHERE name = N'{databaseName}')
CREATE DATABASE [{databaseName}];");
            }

            new SqlServerStorage(Logger, CapOptions, SqlSeverOptions).InitializeAsync().GetAwaiter().GetResult();
            SqlObjectInstalled = true;
        }


        private void DeleteAllData()
        {
            var conn = ConnectionUtil.GetConnectionString();
            using (var connection = new SqlConnection(conn))
            {
                var commands = new[] {
                    "DISABLE TRIGGER ALL ON ?",
                    "ALTER TABLE ? NOCHECK CONSTRAINT ALL",
                    "DELETE FROM ?",
                    "ALTER TABLE ? CHECK CONSTRAINT ALL",
                    "ENABLE TRIGGER ALL ON ?"
                };

                foreach (var command in commands)
                {
                    connection.Execute(
                        "sp_MSforeachtable",
                        new { command1 = command },
                        commandType: CommandType.StoredProcedure);
                }
            }
        }
    }
}