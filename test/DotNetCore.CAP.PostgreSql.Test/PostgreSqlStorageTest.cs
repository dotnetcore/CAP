using Dapper;
using Xunit;

namespace DotNetCore.CAP.PostgreSql.Test
{
    [Collection("PostgreSql")]
    public class PostgreSqlStorageTest : DatabaseTestHost
    {
        private readonly string _dbName;
        private readonly string _masterDbConnectionString;

        public PostgreSqlStorageTest()
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
                var sql = $@"SELECT datname FROM pg_database WHERE datname = '{databaseName}'";
                var result = connection.QueryFirstOrDefault<string>(sql);
                Assert.NotNull(result);
                Assert.True(databaseName.Equals(result, System.StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Theory]
        [InlineData("published")]
        [InlineData("received")]
        public void DatabaseTable_IsExists(string tableName)
        {
            using (var connection = ConnectionUtil.CreateConnection(ConnectionUtil.GetConnectionString()))
            {
                var sql = $"SELECT table_name FROM information_schema.tables WHERE table_schema = 'cap' and table_catalog = '{_dbName}' AND table_name = '{tableName}'";
                var result = connection.QueryFirstOrDefault<string>(sql);
                Assert.NotNull(result);
                Assert.Equal(tableName, result);
            }
        }
    }
}