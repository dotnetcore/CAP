using Dapper;
using Xunit;

namespace DotNetCore.CAP.MySql.Test
{
    [Collection("MySql")]
    public class MySqlStorageTest : DatabaseTestHost
    {
        private readonly string _dbName;
        private readonly string _masterDbConnectionString;

        public MySqlStorageTest()
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
                var sql = $@"SELECT SCHEMA_NAME FROM SCHEMATA WHERE SCHEMA_NAME = '{databaseName}'";
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
                var sql = $"SELECT TABLE_NAME FROM `TABLES` WHERE TABLE_SCHEMA='{_dbName}' AND TABLE_NAME = '{tableName}'";
                var result = connection.QueryFirstOrDefault<string>(sql);
                Assert.NotNull(result);
                Assert.Equal(tableName, result);
            }
        }
    }
}