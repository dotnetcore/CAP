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
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB;

public class MongoDBDataStorage : IDataStorage
{
    private readonly IOptions<CapOptions> _capOptions;
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IOptions<MongoDBOptions> _options;
    private readonly ISerializer _serializer;

    public MongoDBDataStorage(
        IOptions<CapOptions> capOptions,
        IOptions<MongoDBOptions> options,
        IMongoClient client,
        ISerializer serializer)
    {
        _capOptions = capOptions;
        _options = options;
        _client = client;
        _database = _client.GetDatabase(_options.Value.DatabaseName);
        _serializer = serializer;
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
    {
        var collection = _database.GetCollection<Lock>(_options.Value.LockCollection);
        using var session = await _client.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
        var transactionOptions = new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority);
        session.StartTransaction(transactionOptions);
        try
        {
            var opResult = await collection.UpdateOneAsync(session, model => model.Key == key && model.LastLockTime < DateTime.Now.Subtract(ttl),
                Builders<Lock>.Update.Set(model => model.Instance, instance).Set(model => model.LastLockTime, DateTime.Now), null, token);
            var isAcquired = opResult.IsModifiedCountAvailable && opResult.ModifiedCount > 0;
            await session.CommitTransactionAsync(token).ConfigureAwait(false);
            return isAcquired;
        }
        catch (Exception)
        {
            await session.AbortTransactionAsync(token).ConfigureAwait(false);
            return false;
        }
    }

    public async Task ReleaseLockAsync(string key, string instance, CancellationToken token = default)
    {
        var collection = _database.GetCollection<Lock>(_options.Value.LockCollection);
        await collection.UpdateOneAsync(
            model => model.Key == key && model.Instance == instance,
            Builders<Lock>.Update.Set(model => model.Instance, "")
                .Set(model => model.LastLockTime, DateTime.MinValue), null, token).ConfigureAwait(false);
    }

    public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
    {
        var collection = _database.GetCollection<Lock>(_options.Value.LockCollection);
        var filter = Builders<Lock>.Filter.Where(it => it.Key == key && it.Instance == instance);
        var pipeline = new EmptyPipelineDefinition<Lock>()
            .AppendStage<Lock, Lock, Lock>($"{{$set:{{LastLockTime:{{$add:[ \"$LastLockTime\",  {ttl.TotalMilliseconds} ]}}}}}}");
        var update = Builders<Lock>.Update.Pipeline(pipeline);
        await collection.UpdateOneAsync(filter, update, cancellationToken: token).ConfigureAwait(false);
    }

    public async Task ChangePublishStateToDelayedAsync(string[] ids)
    {
        var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);
        var updateDef = Builders<PublishedMessage>.Update.Set(x => x.StatusName, nameof(StatusName.Delayed));
        var filter = Builders<PublishedMessage>.Filter.In(x => x.Id, ids.Select(long.Parse));

        await collection.UpdateManyAsync(filter, updateDef).ConfigureAwait(false);
    }

    public async Task ChangePublishStateAsync(MediumMessage message, StatusName state, object? transaction = null)
    {
        var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);

        var updateDef = Builders<PublishedMessage>.Update
            .Set(x => x.Content, _serializer.Serialize(message.Origin))
            .Set(x => x.Retries, message.Retries)
            .Set(x => x.ExpiresAt, message.ExpiresAt)
            .Set(x => x.StatusName, state.ToString("G"));

        if (transaction != null)
        {
            var session = transaction as IClientSessionHandle;
            await collection.UpdateOneAsync(session, x => x.Id == long.Parse(message.DbId), updateDef).ConfigureAwait(false);
        }
        else
        {
            await collection.UpdateOneAsync(x => x.Id == long.Parse(message.DbId), updateDef).ConfigureAwait(false);
        }
    }

    public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
    {
        var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);

        var updateDef = Builders<ReceivedMessage>.Update
            .Set(x => x.Content, _serializer.Serialize(message.Origin))
            .Set(x => x.Retries, message.Retries)
            .Set(x => x.ExpiresAt, message.ExpiresAt)
            .Set(x => x.StatusName, state.ToString("G"));

        await collection.UpdateOneAsync(x => x.Id == long.Parse(message.DbId), updateDef).ConfigureAwait(false);
    }

    public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object? dbTransaction = null)
    {
        var insertOptions = new InsertOneOptions { BypassDocumentValidation = false };

        var message = new MediumMessage
        {
            DbId = content.GetId(),
            Origin = content,
            Content = _serializer.Serialize(content),
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
            await collection.InsertOneAsync(store, insertOptions).ConfigureAwait(false);
        }
        else
        {
            var dbTrans = dbTransaction as IClientSessionHandle;
            await collection.InsertOneAsync(dbTrans, store, insertOptions).ConfigureAwait(false);
        }

        return message;
    }

    public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
    {
        var collection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);

        var store = new ReceivedMessage
        {
            Id = SnowflakeId.Default().NextId(),
            Group = group,
            Name = name,
            Content = content,
            Added = DateTime.Now,
            ExpiresAt = DateTime.Now.AddSeconds(_capOptions.Value.FailedMessageExpiredAfter),
            Retries = _capOptions.Value.FailedRetryCount,
            Version = _capOptions.Value.Version,
            StatusName = nameof(StatusName.Failed)
        };

        await collection.InsertOneAsync(store).ConfigureAwait(false);
    }

    public async Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message message)
    {
        var mdMessage = new MediumMessage
        {
            DbId = SnowflakeId.Default().NextId().ToString(),
            Origin = message,
            Added = DateTime.Now,
            ExpiresAt = null,
            Retries = 0
        };
        var content = _serializer.Serialize(mdMessage.Origin);

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

        await collection.InsertOneAsync(store).ConfigureAwait(false);

        return mdMessage;
    }

    public async Task<int> DeleteExpiresAsync(string collection, DateTime timeout, int batchCount = 1000, CancellationToken cancellationToken = default)
    {
        if (collection == _options.Value.PublishedCollection)
        {
            var publishedCollection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);
            var ret = await publishedCollection.DeleteManyAsync(x => x.ExpiresAt < timeout && (x.StatusName == nameof(StatusName.Succeeded) || x.StatusName == nameof(StatusName.Failed)), cancellationToken)
                .ConfigureAwait(false);
            return (int)ret.DeletedCount;
        }
        else
        {
            var receivedCollection = _database.GetCollection<ReceivedMessage>(_options.Value.ReceivedCollection);
            var ret = await receivedCollection.DeleteManyAsync(x => x.ExpiresAt < timeout && (x.StatusName == nameof(StatusName.Succeeded) || x.StatusName == nameof(StatusName.Failed)), cancellationToken)
                .ConfigureAwait(false);
            return (int)ret.DeletedCount;
        }
    }

    public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
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
            .ToListAsync().ConfigureAwait(false);
        return queryResult.Select(x => new MediumMessage
        {
            DbId = x.Id.ToString(),
            Origin = _serializer.Deserialize(x.Content)!,
            Retries = x.Retries,
            Added = x.Added
        }).ToList();

    }

    public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
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
            .ToListAsync().ConfigureAwait(false);
        return queryResult.Select(x => new MediumMessage
        {
            DbId = x.Id.ToString(),
            Origin = _serializer.Deserialize(x.Content)!,
            Retries = x.Retries,
            Added = x.Added
        }).ToList();
    }

    public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask, CancellationToken token = default)
    {
        var collection = _database.GetCollection<PublishedMessage>(_options.Value.PublishedCollection);

        var update = Builders<PublishedMessage>.Update.Set(x => x._lockToken, ObjectId.GenerateNewId());

        var filter = Builders<PublishedMessage>.Filter.Where(x => x.Version == _capOptions.Value.Version
                                                     && ((x.StatusName == nameof(StatusName.Delayed) && x.ExpiresAt < DateTime.Now.AddMinutes(2))
                                                         ||
                                                         (x.StatusName == nameof(StatusName.Queued) && x.ExpiresAt < DateTime.Now.AddMinutes(-1)))
                                                     );

        using var timeoutTs = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedTs = CancellationTokenSource.CreateLinkedTokenSource(timeoutTs.Token, token);

        using var session = await _client.StartSessionAsync(cancellationToken: token).ConfigureAwait(false);
        var transactionOptions = new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority);
        session.StartTransaction(transactionOptions);

        while (!timeoutTs.IsCancellationRequested)
        {
            try
            {
                try
                {
                    await collection.UpdateManyAsync(session, filter, update, cancellationToken: linkedTs.Token).ConfigureAwait(false);

                    var queryResult = await collection.Find(session, filter).ToListAsync(linkedTs.Token).ConfigureAwait(false);

                    var result = queryResult.Select(x => new MediumMessage
                    {
                        DbId = x.Id.ToString(),
                        Origin = _serializer.Deserialize(x.Content)!,
                        Retries = x.Retries,
                        Added = x.Added,
                        ExpiresAt = x.ExpiresAt
                    }).ToList();

                    await scheduleTask(session, result).ConfigureAwait(false);

                    await session.CommitTransactionAsync(token).ConfigureAwait(false);

                    break;
                }
                catch (MongoCommandException e) when (e.HasErrorLabel("TransientTransactionError") && e.CodeName == "WriteConflict")
                {
                    await session.AbortTransactionAsync(linkedTs.Token).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(5, 20)), linkedTs.Token).ConfigureAwait(false);
                    session.StartTransaction(transactionOptions);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException && timeoutTs.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public IMonitoringApi GetMonitoringApi()
    {
        return new MongoDBMonitoringApi(_client, _options,_serializer);
    }
}