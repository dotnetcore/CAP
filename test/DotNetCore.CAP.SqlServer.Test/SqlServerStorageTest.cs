using Xunit;
using Dapper;

namespace DotNetCore.CAP.SqlServer.Test
{
    public class SqlServerStorageTest : DatabaseTestHost
    {
        [Fact]
        public void Database_IsExists()
        {
            var master = ConnectionUtil.GetMasterConnectionString();
            using (var connection = ConnectionUtil.CreateConnection(master))
            {
                var databaseName = ConnectionUtil.GetDatabaseName();
                var sql = $@"
IF EXISTS (SELECT * FROM sysdatabases WHERE name = N'{databaseName}')  
SELECT 'True'
ELSE
SELECT 'False'";
                var result = connection.QueryFirst<bool>(sql);
                Assert.Equal(true, result);
            }
        }

        [Fact]
        public void DatabaseTable_Published_IsExists()
        {
            using (var connection = ConnectionUtil.CreateConnection())
            {
                var sql = @"
IF OBJECT_ID(N'[Cap].[Published]',N'U') IS NOT NULL
SELECT 'True'
ELSE
SELECT 'False'";
                var result = connection.QueryFirst<bool>(sql);
                Assert.Equal(true, result);
            }
        }

        [Fact]
        public void DatabaseTable_Queue_IsExists()
        {
            using (var connection = ConnectionUtil.CreateConnection())
            {
                var sql = @"
IF OBJECT_ID(N'[Cap].[Queue]',N'U') IS NOT NULL
SELECT 'True'
ELSE
SELECT 'False'";
                var result = connection.QueryFirst<bool>(sql);
                Assert.Equal(true, result);
            }
        }

        [Fact]
        public void DatabaseTable_Received_IsExists()
        {
            using (var connection = ConnectionUtil.CreateConnection())
            {
                var sql = @"
IF OBJECT_ID(N'[Cap].[Received]',N'U') IS NOT NULL
SELECT 'True'
ELSE
SELECT 'False'";
                var result = connection.QueryFirst<bool>(sql);
                Assert.Equal(true, result);
            }
        }
    }
}
