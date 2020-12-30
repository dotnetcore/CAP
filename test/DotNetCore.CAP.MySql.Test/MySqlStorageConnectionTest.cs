using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.MySql.Test
{
    [Collection("MySql")]
    public class MySqlStorageConnectionTest : DatabaseTestHost
    {
        private readonly MySqlDataStorage _storage;

        public MySqlStorageConnectionTest()
        {
            var serializer = GetService<ISerializer>();
            var options = GetService<IOptions<MySqlOptions>>();
            var capOptions = GetService<IOptions<CapOptions>>();
            var initializer = GetService<IStorageInitializer>();
            _storage = new MySqlDataStorage(options, capOptions, initializer, serializer);
        }

        [Fact]
        public void StorageMessageTest()
        {
            var msgId = SnowflakeId.Default().NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = _storage.StoreMessage("test.name", message);
            Assert.NotNull(mdMessage);
        }

        [Fact]
        public void StoreReceivedMessageTest()
        {
            var msgId = SnowflakeId.Default().NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = _storage.StoreReceivedMessage("test.name", "test.group", message);
            Assert.NotNull(mdMessage);
        }

        [Fact]
        public void StoreReceivedExceptionMessageTest()
        {
            _storage.StoreReceivedExceptionMessage("test.name", "test.group", "");
        }

        [Fact]
        public async Task ChangePublishStateTest()
        {
            var msgId = SnowflakeId.Default().NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = _storage.StoreMessage("test.name", message);

            await _storage.ChangePublishStateAsync(mdMessage, StatusName.Succeeded);
        }

        [Fact]
        public async Task ChangeReceiveStateTest()
        {
            var msgId = SnowflakeId.Default().NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = _storage.StoreMessage("test.name", message);

            await _storage.ChangeReceiveStateAsync(mdMessage, StatusName.Succeeded);
        }
    }
}