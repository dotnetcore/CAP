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
    public class CapPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly MongoDBOptions _options;

        public CapPublisher(IServiceProvider provider, MongoDBOptions options)
            : base(provider)
        {
            _options = options;
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override Task ExecuteAsync(CapPublishedMessage message, ICapTransaction transaction,
            CancellationToken cancel = default(CancellationToken))
        {
            var dbTrans = (IClientSessionHandle)transaction.DbTransaction;

            var collection = dbTrans.Client
                .GetDatabase(_options.DatabaseName)
                .GetCollection<CapPublishedMessage>(_options.PublishedCollection);

            var insertOptions = new InsertOneOptions { BypassDocumentValidation = false };
            return collection.InsertOneAsync(dbTrans, message, insertOptions, cancel);
        }

        protected override object GetDbTransaction()
        {
            var client = ServiceProvider.GetRequiredService<IMongoClient>();
            var session = client.StartSession(new ClientSessionOptions());
            session.StartTransaction();
            return session;
        }
    }
}