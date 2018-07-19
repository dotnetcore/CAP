using System;
using System.Data;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class CapPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly IMongoClient _client;
        private readonly MongoDBOptions _options;
        private readonly IMongoDatabase _database;
        private bool _isInTransaction = true;

        public CapPublisher(ILogger<CapPublisherBase> logger, IDispatcher dispatcher,
        IMongoClient client, MongoDBOptions options, IServiceProvider provider)
        : base(logger, dispatcher)
        {
            _client = client;
            _options = options;
            _database = client.GetDatabase(_options.Database);
            ServiceProvider = provider;
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);
            message.Id = await new MongoDBUtil().GetNextSequenceValueAsync(_database, _options.Published);
            collection.InsertOne(message);
            Enqueue(message);
        }

        protected override int Execute(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message)
        {
            throw new System.NotImplementedException("Not work for MongoDB");
        }

        protected override Task<int> ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message)
        {
            throw new System.NotImplementedException("Not work for MongoDB");
        }

        protected override void PrepareConnectionForEF()
        {
            throw new System.NotImplementedException("Not work for MongoDB");
        }

        public override void PublishWithMongoSession<T>(string name, T contentObj, object mongoSession = null, string callbackName = null)
        {
            var session = mongoSession as IClientSessionHandle;
            if (session == null)
            {
                _isInTransaction = false;
            }

            PublishWithSession<T>(name, contentObj, session, callbackName);
        }

        public override async Task PublishWithMongoSessionAsync<T>(string name, T contentObj, object mongoSession = null, string callbackName = null)
        {
            var session = mongoSession as IClientSessionHandle;
            if (session == null)
            {
                _isInTransaction = false;
            }

            await PublishWithSessionAsync<T>(name, contentObj, session, callbackName);
        }

        private void PublishWithSession<T>(string name, T contentObj, IClientSessionHandle session, string callbackName)
        {
            Guid operationId = default(Guid);

            var content = Serialize(contentObj, callbackName);

            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            try
            {
                operationId = s_diagnosticListener.WritePublishMessageStoreBefore(message);
                var id = Execute(session, message);

                if (!_isInTransaction && id > 0)
                {
                    _logger.LogInformation($"message [{message}] has been persisted in the database.");
                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);
                    message.Id = id;
                    Enqueue(message);
                }
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, "An exception was occurred when publish message. message:" + name);
                s_diagnosticListener.WritePublishMessageStoreError(operationId, message, e);
                throw;
            }
        }

        private int Execute(IClientSessionHandle session, CapPublishedMessage message)
        {
            message.Id = new MongoDBUtil().GetNextSequenceValue(_database, _options.Published, session);

            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);
            if (_isInTransaction)
            {
                collection.InsertOne(session, message);
            }
            else
            {
                collection.InsertOne(message);
            }
            return message.Id;
        }


        private async Task PublishWithSessionAsync<T>(string name, T contentObj, IClientSessionHandle session, string callbackName)
        {
            Guid operationId = default(Guid);
            var content = Serialize(contentObj, callbackName);

            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            try
            {
                operationId = s_diagnosticListener.WritePublishMessageStoreBefore(message);

                var id = await ExecuteAsync(session, message);

                if (!_isInTransaction && id > 0)
                {
                    _logger.LogInformation($"message [{message}] has been persisted in the database.");
                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);

                    message.Id = id;

                    Enqueue(message);
                }
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, "An exception was occurred when publish message async. exception message:" + name);
                s_diagnosticListener.WritePublishMessageStoreError(operationId, message, e);
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<int> ExecuteAsync(IClientSessionHandle session, CapPublishedMessage message)
        {
            message.Id = await new MongoDBUtil().GetNextSequenceValueAsync(_database, _options.Published, session);
            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);
            if (_isInTransaction)
            {
                await collection.InsertOneAsync(session, message);
            }
            else
            {
                await collection.InsertOneAsync(message);
            }
            return message.Id;
        }
    }
}