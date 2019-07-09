using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using DotNetCore.CAP.SqlServer.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DotNetCore.CAP.SqlServer.Test
{
    public abstract class DatabaseTestHost : IDisposable
    {
        protected ILogger<SqlServerStorage> Logger;
        protected IOptions<CapOptions> CapOptions;
        protected IOptions<SqlServerOptions> SqlSeverOptions;
        protected DiagnosticProcessorObserver DiagnosticProcessorObserver;

        public bool SqlObjectInstalled;

        protected DatabaseTestHost()
        {
            Logger = new Mock<ILogger<SqlServerStorage>>().Object;

            var capOptions = new Mock<IOptions<CapOptions>>();
            capOptions.Setup(x => x.Value).Returns(new CapOptions());
            CapOptions = capOptions.Object;

            var options = new Mock<IOptions<SqlServerOptions>>();
            options.Setup(x => x.Value).Returns(new SqlServerOptions { ConnectionString = ConnectionUtil.GetConnectionString() });
            SqlSeverOptions = options.Object;

            DiagnosticProcessorObserver = new DiagnosticProcessorObserver(new Mock<IDispatcher>().Object);

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

            new SqlServerStorage(Logger, CapOptions, SqlSeverOptions, DiagnosticProcessorObserver).InitializeAsync().GetAwaiter().GetResult();
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