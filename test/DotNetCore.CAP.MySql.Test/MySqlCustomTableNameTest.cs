using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.MySql.Test
{
    [Collection("MySql")]
    public class MySqlCustomTableNameTest : IDisposable
    {
        private const string CustomTableNamePrefix = "cap_custom";
        private const string CustomPublishedTableName = "CustomPublished";
        private const string CustomReceivedTableName = "CustomReceived";
        private const string CustomLockTableName = "CustomLock";

        private readonly ServiceProvider _provider;
        private readonly IStorageInitializer _initializer;

        public MySqlCustomTableNameTest()
        {
            EnsureDatabaseExists();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            services.AddOptions<CapOptions>().Configure(x => x.UseStorageLock = true);
            services.Configure<MySqlOptions>(x =>
            {
                x.ConnectionString = ConnectionUtil.GetConnectionString();
                x.TableNamePrefix = CustomTableNamePrefix;
                x.PublishedTableName = CustomPublishedTableName;
                x.ReceivedTableName = CustomReceivedTableName;
                x.LockTableName = CustomLockTableName;
            });
            services.AddSingleton<MySqlDataStorage>();
            services.AddSingleton<IStorageInitializer, MySqlStorageInitializer>();
            services.AddSingleton<ISerializer, JsonUtf8Serializer>();
            services.AddSingleton<ISnowflakeId>(_ => new SnowflakeId(10));

            _provider = services.BuildServiceProvider();
            _initializer = _provider.GetRequiredService<IStorageInitializer>();
            _initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void EnsureDatabaseExists()
        {
            var databaseName = ConnectionUtil.GetDatabaseName();
            using var connection = ConnectionUtil.CreateConnection(ConnectionUtil.GetMasterConnectionString());
            connection.Execute($"CREATE DATABASE IF NOT EXISTS `{databaseName}`;");
        }

        [Fact]
        public void CustomTableNames_AreReturnedByStorageInitializer()
        {
            Assert.Equal($"{CustomTableNamePrefix}.{CustomPublishedTableName}", _initializer.GetPublishedTableName());
            Assert.Equal($"{CustomTableNamePrefix}.{CustomReceivedTableName}", _initializer.GetReceivedTableName());
            Assert.Equal($"{CustomTableNamePrefix}.{CustomLockTableName}", _initializer.GetLockTableName());
        }

        [Theory]
        [InlineData(CustomPublishedTableName)]
        [InlineData(CustomReceivedTableName)]
        [InlineData(CustomLockTableName)]
        public void CustomTable_IsCreatedWithCustomPrefix(string tableName)
        {
            using var connection = ConnectionUtil.CreateConnection();
            var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=@Schema AND TABLE_NAME=@TableName";
            var fullTableName = $"{CustomTableNamePrefix}.{tableName}";
            var result = connection.QueryFirstOrDefault<string>(sql,
                new { Schema = ConnectionUtil.GetDatabaseName(), TableName = fullTableName });
            Assert.Equal(fullTableName, result);
        }

        [Fact]
        public void DefaultNamedTables_AreNotCreatedWithCustomPrefix()
        {
            using var connection = ConnectionUtil.CreateConnection();
            var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=@Schema AND TABLE_NAME=@TableName";
            var result = connection.QueryFirstOrDefault<string>(sql,
                new { Schema = ConnectionUtil.GetDatabaseName(), TableName = $"{CustomTableNamePrefix}.published" });
            Assert.Null(result);
        }

        [Fact]
        public async Task StoreMessageAsync_PersistsRowInCustomPublishedTable()
        {
            var storage = _provider.GetRequiredService<MySqlDataStorage>();
            var snowflakeId = _provider.GetRequiredService<ISnowflakeId>();

            var msgId = snowflakeId.NextId().ToString();
            var header = new Dictionary<string, string> { [Headers.MessageId] = msgId };
            var message = new Message(header, null);

            var mdMessage = await storage.StoreMessageAsync("test.custom.name", message);

            using var connection = ConnectionUtil.CreateConnection();
            var sql = $"SELECT COUNT(1) FROM `{CustomTableNamePrefix}.{CustomPublishedTableName}` WHERE `Id`=@Id";
            var count = connection.QueryFirstOrDefault<int>(sql, new { Id = mdMessage.DbId });
            Assert.Equal(1, count);
        }

        public void Dispose()
        {
            using (var connection = ConnectionUtil.CreateConnection())
            {
                connection.Execute($@"
DROP TABLE IF EXISTS `{CustomTableNamePrefix}.{CustomPublishedTableName}`;
DROP TABLE IF EXISTS `{CustomTableNamePrefix}.{CustomReceivedTableName}`;
DROP TABLE IF EXISTS `{CustomTableNamePrefix}.{CustomLockTableName}`;");
            }

            _provider.Dispose();
        }
    }
}
