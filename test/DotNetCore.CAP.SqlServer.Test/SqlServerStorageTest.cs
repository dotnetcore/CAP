using Dapper;
using Xunit;

namespace DotNetCore.CAP.SqlServer.Test
{
    [Collection("sqlserver")]
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
                Assert.True(result);
            }
        }

        [Theory]
        [InlineData("[Cap].[Published]")]
        [InlineData("[Cap].[Received]")]
        public void DatabaseTable_IsExists(string tableName)
        {
            using (var connection = ConnectionUtil.CreateConnection())
            {
                var sql = $@"
IF OBJECT_ID(N'{tableName}',N'U') IS NOT NULL
SELECT 'True'
ELSE
SELECT 'False'";
                var result = connection.QueryFirst<bool>(sql);
                Assert.True(result);
            }
        }
    }
}