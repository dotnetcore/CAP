// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Data;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Mocker
{
    public class MockCapPublisher : ICapPublisher
    {
        public void Publish<T>(string name, T contentObj, string callbackName = null)
        {
        }

        public void Publish<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null)
        {
        }

        public Task PublishAsync<T>(string name, T contentObj, string callbackName = null)
        {
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null)
        {
            return Task.CompletedTask;
        }

        public void PublishWithMongo<T>(string name, T contentObj, IMongoTransaction mongoTransaction = null, string callbackName = null)
        {
        }

        public Task PublishWithMongoAsync<T>(string name, T contentObj, IMongoTransaction mongoTransaction = null, string callbackName = null)
        {
            return Task.CompletedTask;
        }
    }
}