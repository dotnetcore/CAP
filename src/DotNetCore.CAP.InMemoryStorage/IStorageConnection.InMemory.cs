// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.InMemoryStorage
{
    public class InMemoryStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;

        public InMemoryStorageConnection(IOptions<CapOptions> capOptions)
        {
            _capOptions = capOptions.Value;

            PublishedMessages = new List<CapPublishedMessage>();
            ReceivedMessages = new List<CapReceivedMessage>();
        }

        internal List<CapPublishedMessage> PublishedMessages { get; }

        internal List<CapReceivedMessage> ReceivedMessages { get; }

        public IStorageTransaction CreateTransaction()
        {
           return new InMemoryStorageTransaction(this);
        }

        public Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        {
            return PublishedMessages.ToAsyncEnumerable().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            return await PublishedMessages.ToAsyncEnumerable()
                .Where(x => x.Retries < _capOptions.FailedRetryCount
                         && x.Added < DateTime.Now.AddSeconds(-10)
                         && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                .Take(200)
                .ToListAsync();
        }

        public void StoreReceivedMessage(CapReceivedMessage message)
        {
            ReceivedMessages.Add(message);
        }

        public Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        {
            return ReceivedMessages.ToAsyncEnumerable().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            return await ReceivedMessages.ToAsyncEnumerable()
                .Where(x => x.Retries < _capOptions.FailedRetryCount
                            && x.Added < DateTime.Now.AddSeconds(-10)
                            && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                .Take(200)
                .ToListAsync();
        }

        public bool ChangePublishedState(long messageId, string state)
        {
            var msg = PublishedMessages.First(x => x.Id == messageId);
            msg.Retries++;
            msg.ExpiresAt = null;
            msg.StatusName = state;
            return true;
        }

        public bool ChangeReceivedState(long messageId, string state)
        {
            var msg = ReceivedMessages.First(x => x.Id == messageId);
            msg.Retries++;
            msg.ExpiresAt = null;
            msg.StatusName = state;
            return true;
        }
    }
}