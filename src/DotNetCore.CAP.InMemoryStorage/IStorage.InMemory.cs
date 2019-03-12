// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;

namespace DotNetCore.CAP.InMemoryStorage
{
    public class InMemoryStorage : IStorage
    {
        private readonly IStorageConnection _connection;

        public InMemoryStorage(IStorageConnection connection)
        {
            _connection = connection;
        }

        public IStorageConnection GetConnection()
        {
            return _connection;
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new InMemoryMonitoringApi(this);
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}