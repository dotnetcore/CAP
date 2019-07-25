// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly IMongoClient _client;
        private readonly MongoDBOptions _options;

        public MongoDBPublisher(IServiceProvider provider) : base(provider)
        {
            _options = provider.GetService<IOptions<MongoDBOptions>>().Value;
            _client = ServiceProvider.GetRequiredService<IMongoClient>();
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override Task ExecuteAsync(CapPublishedMessage message,
            ICapTransaction transaction = null,
            CancellationToken cancel = default)
        {
            var insertOptions = new InsertOneOptions { BypassDocumentValidation = false };

            var collection = _client
                .GetDatabase(_options.DatabaseName)
                .GetCollection<PublishedMessage>(_options.PublishedCollection);

            var store = new PublishedMessage()
            {
                Id = message.Id,
                Name = message.Name,
                Content = message.Content,
                Added = message.Added,
                StatusName = message.StatusName,
                ExpiresAt = message.ExpiresAt,
                Retries = message.Retries,
                Version = _options.Version,
            };

            if (transaction == null)
            {
                return collection.InsertOneAsync(store, insertOptions, cancel);
            }

            var dbTrans = (IClientSessionHandle)transaction.DbTransaction;
            return collection.InsertOneAsync(dbTrans, store, insertOptions, cancel);
        }
    }
}