using System.Data;
using System.Data.SqlClient;
using System.Threading;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.SqlServer.Test
{
    public abstract class DatabaseTestHost : TestHost
    {
        private static bool _sqlObjectInstalled;

        protected override void PostBuildServices()
        {
            base.PostBuildServices();
            InitializeDatabase();
        }

        public override void Dispose()
        {
            DeleteAllData();
            base.Dispose();
        }

        private void InitializeDatabase()
        {
            if (!_sqlObjectInstalled)
            {
                using (CreateScope())
                {
                    var storage = GetService<SqlServerStorage>();
                    var token = new CancellationTokenSource().Token;
                    CreateDatabase();
                    storage.InitializeAsync(token).Wait();
                    _sqlObjectInstalled = true;
                }
            }
        }

        private void CreateDatabase()
        {
            var masterConn = ConnectionUtil.GetMasterConnectionString();
            var databaseName = ConnectionUtil.GetDatabaseName();
            using (var connection = ConnectionUtil.CreateConnection(masterConn))
            {
                connection.Execute($@"
IF NOT EXISTS (SELECT * FROM sysdatabases WHERE name = N'{databaseName}')  
CREATE DATABASE [{databaseName}];");
            }
        }

        private void DeleteAllData()
        {
            using (CreateScope())
            {
                var context = GetService<TestDbContext>();

                var commands = new[]
                {
                    "DISABLE TRIGGER ALL ON ?",
                    "ALTER TABLE ? NOCHECK CONSTRAINT ALL",
                    "DELETE FROM ?",
                    "ALTER TABLE ? CHECK CONSTRAINT ALL",
                    "ENABLE TRIGGER ALL ON ?"
                };
                foreach (var command in commands)
                {
                    context.Database.GetDbConnection().Execute(
                        "sp_MSforeachtable",
                        new { command1 = command },
                        commandType: CommandType.StoredProcedure);
                }
            }
        }
    }
}