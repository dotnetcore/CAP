// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.InMemoryStorage
{
    internal class InMemoryCollectProcessor : ICollectProcessor
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public InMemoryCollectProcessor(ILogger<InMemoryCollectProcessor> logger)
        {
            _logger = logger;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug($"Collecting expired data from memory list.");

            var connection = (InMemoryStorageConnection)context.Provider.GetService<IStorageConnection>();

            connection.PublishedMessages.RemoveAll(x => x.ExpiresAt < DateTime.Now);
            connection.ReceivedMessages.RemoveAll(x => x.ExpiresAt < DateTime.Now);

            await context.WaitAsync(_waitingInterval);
        }
    }
}