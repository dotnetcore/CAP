// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.InMemoryStorage
{
    internal class InMemoryStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;

        public InMemoryStorage(IOptions<CapOptions> capOptions)
        {
            _capOptions = capOptions;
        }

        public static IList<MemoryMessage> PublishedMessages { get; } = new List<MemoryMessage>();

        public static IList<MemoryMessage> ReceivedMessages { get; } = new List<MemoryMessage>();

        public Task ChangePublishStateAsync(MediumMessage message, StatusName state)
        {
            PublishedMessages.First(x => x.DbId == message.DbId).StatusName = state;
            return Task.CompletedTask;
        }

        public Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            ReceivedMessages.First(x => x.DbId == message.DbId).StatusName = state;
            return Task.CompletedTask;
        }

        public Task<MediumMessage> StoreMessageAsync(string name, Message content, object dbTransaction = null,
            CancellationToken cancellationToken = default)
        {
            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = StringSerializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            PublishedMessages.Add(new MemoryMessage()
            {
                DbId = message.DbId,
                Name = name,
                Content = message.Content,
                Retries = message.Retries,
                Added = message.Added,
                ExpiresAt = message.ExpiresAt,
                StatusName = StatusName.Scheduled
            });

            return Task.FromResult(message);
        }

        public Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
        {
            ReceivedMessages.Add(new MemoryMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Group = group,
                Name = name,
                Content = content,
                Retries = _capOptions.Value.FailedRetryCount,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(15),
                StatusName = StatusName.Failed
            });

            return Task.CompletedTask;
        }

        public Task<MediumMessage> StoreReceivedMessageAsync(string name, string @group, Message message)
        {
            var mdMessage = new MediumMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            ReceivedMessages.Add(new MemoryMessage
            {
                DbId = mdMessage.DbId,
                Group = group,
                Name = name,
                Content = StringSerializer.Serialize(mdMessage.Origin),
                Retries = mdMessage.Retries,
                Added = mdMessage.Added,
                ExpiresAt = mdMessage.ExpiresAt,
                StatusName = StatusName.Failed
            });

            return Task.FromResult(mdMessage);
        }

        public Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            var ret = table == nameof(PublishedMessages)
                ? ((List<MemoryMessage>)PublishedMessages).RemoveAll(x => x.ExpiresAt < timeout)
                : ((List<MemoryMessage>)ReceivedMessages).RemoveAll(x => x.ExpiresAt < timeout);
            return Task.FromResult(ret);
        }

        public Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var ret = PublishedMessages
                .Where(x => x.Retries < _capOptions.Value.FailedRetryCount
                            && x.Added < DateTime.Now.AddSeconds(-10)
                            && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                .Take(200)
                .Select(x => (MediumMessage)x);
            return Task.FromResult(ret);
        }

        public Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var ret = ReceivedMessages
                 .Where(x => x.Retries < _capOptions.Value.FailedRetryCount
                             && x.Added < DateTime.Now.AddSeconds(-10)
                             && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                 .Take(200)
                 .Select(x => (MediumMessage)x);
            return Task.FromResult(ret);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new InMemoryMonitoringApi();
        }
    }
}