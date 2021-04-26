// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public partial class MongoDBDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IOptions<MongoDBOptions> _options;
        //private readonly ISerializer _serializer;
        private readonly ISerializerRegistry _serializerRegistry;
        private readonly IMessageSerializer _messageSerializer;

        public MongoDBDataStorage(
            IOptions<CapOptions> capOptions,
            IOptions<MongoDBOptions> options,
            IMongoClient client,
            //ISerializer serializer,
            ISerializerRegistry serializerRegistry)
        {
            _capOptions = capOptions;
            _options = options;
            _client = client;
            _database = _client.GetDatabase(_options.Value.DatabaseName);
            //_serializer = serializer;
            _serializerRegistry = serializerRegistry;
            _messageSerializer = _serializerRegistry.GetMessageSerializer();
        }

        public async Task ChangePublishStateAsync(IMediumMessage message, StatusName state)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());

            var updateDef = Builders<PublishedMessage>.Update
                .Set(x => x.Content, serializer.Serialize(message.Origin))
                .Set(x => x.Retries, message.Retries)
                .Set(x => x.ExpiresAt, message.ExpiresAt)
                .Set(x => x.StatusName, state.ToString("G"));

            await collection.UpdateOneAsync(x => x.Id == long.Parse(message.DbId), updateDef);
        }

        public async Task ChangeReceiveStateAsync(IMediumMessage message, StatusName state)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);
            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());


            var updateDef = Builders<ReceivedMessage>.Update
                .Set(x => x.Content, serializer.Serialize(message.Origin))
                .Set(x => x.Retries, message.Retries)
                .Set(x => x.ExpiresAt, message.ExpiresAt)
                .Set(x => x.StatusName, state.ToString("G"));

            await collection.UpdateOneAsync(x => x.Id == long.Parse(message.DbId), updateDef);
        }

        public IMediumMessage StoreMessage(string name, ICapMessage content, object dbTransaction = null)
        {
            var insertOptions = new InsertOneOptions { BypassDocumentValidation = false };

            var serializer = _serializerRegistry.GetMessageSerializer(content.GetMessageType());

            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = serializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);

            var store = new PublishedMessage
            {
                Id = long.Parse(message.DbId),
                Name = name,
                Content = message.Content,
                Added = message.Added,
                StatusName = nameof(StatusName.Scheduled),
                ExpiresAt = message.ExpiresAt,
                Retries = message.Retries,
                Version = _options.Value.Version
            };

            if (dbTransaction == null)
            {
                collection.InsertOne(store, insertOptions);
            }
            else
            {
                var dbTrans = dbTransaction as IClientSessionHandle;
                collection.InsertOne(dbTrans, store, insertOptions);
            }

            return message;
        }

        public void StoreReceivedExceptionMessage(string name, string group, string content)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);


            var store = new ReceivedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Group = group,
                Name = name,
                Content = content,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(15),
                Retries = _capOptions.Value.FailedRetryCount,
                Version = _capOptions.Value.Version,
                StatusName = nameof(StatusName.Failed)
            };

            collection.InsertOne(store);
        }

        public IMediumMessage StoreReceivedMessage(string name, string group, ICapMessage message)
        {

            var mdMessage = new MediumMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            //var content = _serializer.Serialize(mdMessage.Origin);

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetMessageType());

            var content = serializer.Serialize(mdMessage.Origin);

            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);

            var store = new ReceivedMessage
            {
                Id = long.Parse(mdMessage.DbId),
                Group = group,
                Name = name,
                Content = content,
                Added = mdMessage.Added,
                ExpiresAt = mdMessage.ExpiresAt,
                Retries = mdMessage.Retries,
                Version = _capOptions.Value.Version,
                StatusName = nameof(StatusName.Scheduled)
            };

            collection.InsertOne(store);

            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string collection, DateTime timeout, int batchCount = 1000,
            CancellationToken cancellationToken = default)
        {
            if (collection == _options.Value.PublishedCollection)
            {
                var publishedCollection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);
                var ret = await publishedCollection.DeleteManyAsync(x => x.ExpiresAt < timeout, cancellationToken);
                return (int)ret.DeletedCount;
            }
            else
            {
                var receivedCollection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);
                var ret = await receivedCollection.DeleteManyAsync(x => x.ExpiresAt < timeout, cancellationToken);
                return (int)ret.DeletedCount;
            }
        }

        public async Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);
            var queryResult = await collection
                .Find(x => x.Retries < _capOptions.Value.FailedRetryCount
                           && x.Added < fourMinAgo
                           && x.Version == _capOptions.Value.Version
                           && (x.StatusName == nameof(StatusName.Failed) ||
                               x.StatusName == nameof(StatusName.Scheduled)))
                .Limit(200)
                .ToListAsync();

            var serializer = _serializerRegistry.GetMessageSerializer();

            return queryResult.Select(x => new MediumMessage
            {
                DbId = x.Id.ToString(),
                Origin = serializer.Deserialize(x.Content),
                Retries = x.Retries,
                Added = x.Added
            }).ToList();
        }

        public async Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);
            var queryResult = await collection
                .Find(x => x.Retries < _capOptions.Value.FailedRetryCount
                           && x.Added < fourMinAgo
                           && x.Version == _capOptions.Value.Version
                           && (x.StatusName == nameof(StatusName.Failed) ||
                               x.StatusName == nameof(StatusName.Scheduled)))
                .Limit(200)
                .ToListAsync();

            var serializer = _serializerRegistry.GetMessageSerializer();

            return queryResult.Select(x => new MediumMessage
            {
                DbId = x.Id.ToString(),
                Origin = serializer.Deserialize(x.Content),
                Retries = x.Retries,
                Added = x.Added
            }).ToList();
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new MongoDBMonitoringApi(_client, _options);
        }

    }

    public partial class MongoDBDataStorage : IDataStorage
    {

        public async Task ChangePublishStateAsync<T>(IMediumMessage message, StatusName state)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());

            var updateDef = Builders<PublishedMessage>.Update
                .Set(x => x.Content, serializer.Serialize(message.Origin))
                .Set(x => x.Retries, message.Retries)
                .Set(x => x.ExpiresAt, message.ExpiresAt)
                .Set(x => x.StatusName, state.ToString("G"));

            await collection.UpdateOneAsync(x => x.Id == long.Parse(message.DbId), updateDef);
        }

        public async Task ChangeReceiveStateAsync<T>(IMediumMessage message, StatusName state)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());

            var updateDef = Builders<ReceivedMessage>.Update
                .Set(x => x.Content, serializer.Serialize(message.Origin))
                .Set(x => x.Retries, message.Retries)
                .Set(x => x.ExpiresAt, message.ExpiresAt)
                .Set(x => x.StatusName, state.ToString("G"));

            await collection.UpdateOneAsync(x => x.Id == long.Parse(message.DbId), updateDef);
        }

        public IMediumMessage StoreMessage<T>(string name, ICapMessage content, object dbTransaction = null)
        {
            var insertOptions = new InsertOneOptions { BypassDocumentValidation = false };

            var serializer = _serializerRegistry.GetMessageSerializer(content.GetMessageType());

            var message = new MediumMessage<T>
            {
                DbId = content.GetId(),
                Origin = content,
                Content = serializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);

            var store = new PublishedMessage
            {
                Id = long.Parse(message.DbId),
                Name = name,
                Content = message.Content,
                Added = message.Added,
                StatusName = nameof(StatusName.Scheduled),
                ExpiresAt = message.ExpiresAt,
                Retries = message.Retries,
                Version = _options.Value.Version
            };

            if (dbTransaction == null)
            {
                collection.InsertOne(store, insertOptions);
            }
            else
            {
                var dbTrans = dbTransaction as IClientSessionHandle;
                collection.InsertOne(dbTrans, store, insertOptions);
            }

            return message;
        }

        public void StoreReceivedExceptionMessage<T>(string name, string group, string content)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);

            var store = new ReceivedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Group = group,
                Name = name,
                Content = content,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(15),
                Retries = _capOptions.Value.FailedRetryCount,
                Version = _capOptions.Value.Version,
                StatusName = nameof(StatusName.Failed)
            };

            collection.InsertOne(store);
        }

        public IMediumMessage StoreReceivedMessage<T>(string name, string group, ICapMessage message)
        {
            var mdMessage = new MediumMessage<T>
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };
            var serializer = _serializerRegistry.GetMessageSerializer(message.GetMessageType());

            var content = serializer.Serialize(mdMessage.Origin);

            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);

            var store = new ReceivedMessage
            {
                Id = long.Parse(mdMessage.DbId),
                Group = group,
                Name = name,
                Content = content,
                Added = mdMessage.Added,
                ExpiresAt = mdMessage.ExpiresAt,
                Retries = mdMessage.Retries,
                Version = _capOptions.Value.Version,
                StatusName = nameof(StatusName.Scheduled)
            };

            collection.InsertOne(store);

            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync<T>(string collection, DateTime timeout, int batchCount = 1000,
            CancellationToken cancellationToken = default)
        {
            if (collection == _options.Value.PublishedCollection)
            {
                var publishedCollection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);
                var ret = await publishedCollection.DeleteManyAsync(x => x.ExpiresAt < timeout, cancellationToken);
                return (int)ret.DeletedCount;
            }
            else
            {
                var receivedCollection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);
                var ret = await receivedCollection.DeleteManyAsync(x => x.ExpiresAt < timeout, cancellationToken);
                return (int)ret.DeletedCount;
            }
        }

        public async Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry<T>()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);
            var queryResult = await collection
                .Find(x => x.Retries < _capOptions.Value.FailedRetryCount
                           && x.Added < fourMinAgo
                           && x.Version == _capOptions.Value.Version
                           && (x.StatusName == nameof(StatusName.Failed) ||
                               x.StatusName == nameof(StatusName.Scheduled)))
                .Limit(200)
                .ToListAsync();


            return queryResult.Select(x => new MediumMessage<T>
            {
                DbId = x.Id.ToString(),
                Origin = (IMessage<T>)_messageSerializer.Deserialize(x.Content),
                Retries = x.Retries,
                Added = x.Added
            }).ToList();
        }

        public async Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry<T>()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);
            var queryResult = await collection
                .Find(x => x.Retries < _capOptions.Value.FailedRetryCount
                           && x.Added < fourMinAgo
                           && x.Version == _capOptions.Value.Version
                           && (x.StatusName == nameof(StatusName.Failed) ||
                               x.StatusName == nameof(StatusName.Scheduled)))
                .Limit(200)
                .ToListAsync();
            return queryResult.Select(x => new MediumMessage<T>
            {
                DbId = x.Id.ToString(),
                Origin = (IMessage<T>)_messageSerializer.Deserialize(x.Content),
                Retries = x.Retries,
                Added = x.Added
            }).ToList();
        }

    }
}
