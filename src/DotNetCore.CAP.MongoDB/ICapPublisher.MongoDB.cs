// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly IMongoClient _client;
        private readonly MongoDBOptions _options;

        public MongoDBPublisher(IServiceProvider provider, MongoDBOptions options)
            : base(provider)
        {
            _options = options;
            _client = ServiceProvider.GetRequiredService<IMongoClient>();
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override Task ExecuteAsync(CapPublishedMessage message, ICapTransaction transaction,
            CancellationToken cancel = default(CancellationToken))
        {
            var insertOptions = new InsertOneOptions {BypassDocumentValidation = false};

            var collection = _client
                .GetDatabase(_options.DatabaseName)
                .GetCollection<CapPublishedMessage>(_options.PublishedCollection);

            if (NotUseTransaction)
            {
                return collection.InsertOneAsync(message, insertOptions, cancel);
            }

            var dbTrans = (IClientSessionHandle) transaction.DbTransaction;
            return collection.InsertOneAsync(dbTrans, message, insertOptions, cancel);
        }
    }
}