using System.Threading;
using Dapper;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.MySql.Test
{
    public abstract class DatabaseTestHost : TestHost
    {
        private static bool _sqlObjectInstalled;
        public static object _lock = new object();

        protected override void PostBuildServices()
        {
            base.PostBuildServices();
            lock (_lock)
            {
                if (!_sqlObjectInstalled)
                {
                    InitializeDatabase();
                }
            }
        }

        public override void Dispose()
        {
            DeleteAllData();
            base.Dispose();
        }

        private void InitializeDatabase()
        {
            using (CreateScope())
            {
                var storage = GetService<IStorageInitializer>();
                var token = new CancellationTokenSource().Token;
                CreateDatabase();
                storage.InitializeAsync(token).GetAwaiter().GetResult();
                _sqlObjectInstalled = true;
            }
        }

        private void CreateDatabase()
        {
            var masterConn = ConnectionUtil.GetMasterConnectionString();
            var databaseName = ConnectionUtil.GetDatabaseName();
            using (var connection = ConnectionUtil.CreateConnection(masterConn))
            {
                connection.Execute($@"
DROP DATABASE IF EXISTS `{databaseName}`;
CREATE DATABASE `{databaseName}`;");
            }
        }

        private void DeleteAllData()
        {
            var conn = ConnectionUtil.GetConnectionString();

            using (var connection = ConnectionUtil.CreateConnection(conn))
            {
                connection.Execute($@"
TRUNCATE TABLE `cap.published`;
TRUNCATE TABLE `cap.received`;");
            }
        }
    }
}