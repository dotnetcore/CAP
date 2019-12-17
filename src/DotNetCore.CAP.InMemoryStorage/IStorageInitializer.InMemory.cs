// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.InMemoryStorage
{
    internal class InMemoryStorageInitializer : IStorageInitializer
    {
        public string GetPublishedTableName()
        {
            return nameof(InMemoryStorage.PublishedMessages);
        }

        public string GetReceivedTableName()
        {
            return nameof(InMemoryStorage.ReceivedMessages);
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
             return Task.CompletedTask;
        }
    }
}