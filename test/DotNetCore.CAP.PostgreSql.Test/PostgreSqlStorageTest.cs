using Dapper;
using Xunit;

namespace DotNetCore.CAP.PostgreSql.Test
{
    [Collection("postgresql")]
    public class SqlServerStorageTest : DatabaseTestHost
    {
        private readonly string _masterDbConnectionString;
        private readonly string _dbConnectionString;

        public SqlServerStorageTest()
        {
            _masterDbConnectionString = ConnectionUtil.GetMasterConnectionString();
            _dbConnectionString = ConnectionUtil.GetConnectionString();
        }

        [Fact]
        public void Database_IsExists()
        {
            using (var connection = ConnectionUtil.CreateConnection(_masterDbConnectionString))
            {
                var databaseName = ConnectionUtil.GetDatabaseName();
                var sql = $@"select * from pg_database where datname = '{databaseName}'";
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
            using (var connection = ConnectionUtil.CreateConnection(_dbConnectionString))
            {
                var sql = $"SELECT to_regclass('{tableName}') is not null;";
                var result = connection.QueryFirstOrDefault<bool>(sql);
                Assert.True(result);
            }
        }
    }
}