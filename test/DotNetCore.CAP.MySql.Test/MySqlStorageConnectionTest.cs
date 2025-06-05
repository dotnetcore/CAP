﻿using System.Collections.Generic;
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
        private ISnowflakeId _snowflakeId;

        public MySqlStorageConnectionTest()
        {
            var serializer = GetService<ISerializer>();
            var options = GetService<IOptions<MySqlOptions>>();
            var capOptions = GetService<IOptions<CapOptions>>();
            var initializer = GetService<IStorageInitializer>();
            _snowflakeId = GetService<ISnowflakeId>();
            _storage = new MySqlDataStorage(options, capOptions, initializer, serializer, _snowflakeId);
        }

        [Fact]
        public async Task StorageMessageTest()
        {
            var msgId = _snowflakeId.NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = await _storage.StoreMessageAsync("test.name", message);
            Assert.NotNull(mdMessage);
        }

        [Fact]
        public async Task StoreReceivedMessageTest()
        {
            var msgId = _snowflakeId.NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = await _storage.StoreReceivedMessageAsync("test.name", "test.group", message);
            Assert.NotNull(mdMessage);
        }

        [Fact]
        public async Task StoreReceivedExceptionMessageTest()
        {
            await _storage.StoreReceivedExceptionMessageAsync("test.name", "test.group", "");
        }

        [Fact]
        public async Task ChangePublishStateTest()
        {
            var msgId = _snowflakeId.NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = await _storage.StoreMessageAsync("test.name", message);

            await _storage.ChangePublishStateAsync(mdMessage, StatusName.Succeeded);
        }

        [Fact]
        public async Task ChangeReceiveStateTest()
        {
            var msgId = _snowflakeId.NextId().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);

            var mdMessage = await _storage.StoreReceivedMessageAsync("test.name", "test.group", message);

            await _storage.ChangeReceiveStateAsync(mdMessage, StatusName.Succeeded);
        }
    }
}