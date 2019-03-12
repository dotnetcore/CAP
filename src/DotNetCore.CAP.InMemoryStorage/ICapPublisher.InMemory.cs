// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.InMemoryStorage
{
    public class InMemoryPublisher : CapPublisherBase, ICallbackPublisher
    {
        public InMemoryPublisher(IServiceProvider provider) : base(provider)
        {
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override Task ExecuteAsync(CapPublishedMessage message, ICapTransaction transaction,
            CancellationToken cancel = default(CancellationToken))
        {
            var connection = (InMemoryStorageConnection)ServiceProvider.GetService<IStorageConnection>();

            connection.PublishedMessages.Add(message);

            return Task.CompletedTask;
        }
    }
}