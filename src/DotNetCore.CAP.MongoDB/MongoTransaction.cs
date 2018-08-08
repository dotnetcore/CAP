// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoTransaction : IMongoTransaction
    {
        private readonly IMongoClient _client;
        public MongoTransaction(IMongoClient client)
        {
            _client = client;
        }

        public IClientSessionHandle Session { get; set; }
        public bool AutoCommit { get; set; }

        public async Task<IMongoTransaction> BegeinAsync(bool autoCommit = true)
        {
            AutoCommit = autoCommit;
            Session = await _client.StartSessionAsync();
            Session.StartTransaction();
            return this;
        }

        public IMongoTransaction Begein(bool autoCommit = true)
        {
            AutoCommit = autoCommit;
            Session = _client.StartSession();
            Session.StartTransaction();
            return this;
        }

        public void Dispose()
        {
            Session?.Dispose();
        }
    }

    public class NullMongoTransaction : MongoTransaction
    {
        public NullMongoTransaction(IMongoClient client = null) : base(client)
        {
            AutoCommit = false;
        }
    }

    public static class MongoTransactionExtensions
    {
        public static IClientSessionHandle GetSession(this IMongoTransaction mongoTransaction)
        {
            var trans = mongoTransaction as MongoTransaction;
            return trans?.Session;
        }
    }
}