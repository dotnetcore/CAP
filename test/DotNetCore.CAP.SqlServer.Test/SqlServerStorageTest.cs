using Dapper;
using Xunit;

namespace DotNetCore.CAP.SqlServer.Test
{
    [Collection("SqlServer")]
    public class SqlServerStorageTest : DatabaseTestHost
    {
        private readonly string _dbName;
        private readonly string _masterDbConnectionString;

        public SqlServerStorageTest()
        {
            _dbName = ConnectionUtil.GetDatabaseName();
            _masterDbConnectionString = ConnectionUtil.GetMasterConnectionString();
        }

        [Fact]
        public void Database_IsExists()
        {
            using (var connection = ConnectionUtil.CreateConnection(_masterDbConnectionString))
            {
                var databaseName = ConnectionUtil.GetDatabaseName();
                var sql = $@"select name From dbo.sysdatabases where name= '{databaseName}'";
                var result = connection.QueryFirstOrDefault<string>(sql);
                Assert.NotNull(result);
                Assert.True(databaseName.Equals(result, System.StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Theory]
        [InlineData("cap.published")]
        [InlineData("cap.received")]
        public void DatabaseTable_IsExists(string tableName)
        {
            using (var connection = ConnectionUtil.CreateConnection(_masterDbConnectionString))
            {
                var sql = $"SELECT TABLE_SCHEMA+'.'+TABLE_NAME FROM {_dbName}.INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA+'.'+TABLE_NAME='{tableName}'";
                var result = connection.QueryFirstOrDefault<string>(sql);
                Assert.NotNull(result);
                Assert.True(tableName.Equals(result, System.StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }
}