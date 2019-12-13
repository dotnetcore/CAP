// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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

        public static ConcurrentDictionary<string, MemoryMessage> PublishedMessages { get; } = new ConcurrentDictionary<string, MemoryMessage>();

        public static ConcurrentDictionary<string, MemoryMessage> ReceivedMessages { get; } = new ConcurrentDictionary<string, MemoryMessage>();

        public Task ChangePublishStateAsync(MediumMessage message, StatusName state)
        {
            PublishedMessages[message.DbId].StatusName = state;
            PublishedMessages[message.DbId].ExpiresAt = message.ExpiresAt;
            return Task.CompletedTask;
        }

        public Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            ReceivedMessages[message.DbId].StatusName = state;
            ReceivedMessages[message.DbId].ExpiresAt = message.ExpiresAt;
            return Task.CompletedTask;
        }

        public MediumMessage StoreMessage(string name, Message content, object dbTransaction = null)
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

            PublishedMessages[message.DbId] = new MemoryMessage()
            {
                DbId = message.DbId,
                Name = name,
                Content = message.Content,
                Retries = message.Retries,
                Added = message.Added,
                ExpiresAt = message.ExpiresAt,
                StatusName = StatusName.Scheduled
            };

            return message;
        }

        public void StoreReceivedExceptionMessage(string name, string group, string content)
        {
            var id = SnowflakeId.Default().NextId().ToString();

            ReceivedMessages[id] = new MemoryMessage
            {
                DbId = id,
                Group = group,
                Origin = null,
                Name = name,
                Content = content,
                Retries = _capOptions.Value.FailedRetryCount,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(15),
                StatusName = StatusName.Failed
            };
        }

        public MediumMessage StoreReceivedMessage(string name, string @group, Message message)
        {
            var mdMessage = new MediumMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            ReceivedMessages[mdMessage.DbId] = new MemoryMessage
            {
                DbId = mdMessage.DbId,
                Origin = mdMessage.Origin,
                Group = group,
                Name = name,
                Content = StringSerializer.Serialize(mdMessage.Origin),
                Retries = mdMessage.Retries,
                Added = mdMessage.Added,
                ExpiresAt = mdMessage.ExpiresAt,
                StatusName = StatusName.Scheduled
            };
            return mdMessage;
        }

        public Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            var removed = 0;
            if (table == nameof(PublishedMessages))
            {
                var ids = PublishedMessages.Values.Where(x => x.ExpiresAt < timeout).Select(x => x.DbId).ToList();
                foreach (var id in ids)
                {
                    if (PublishedMessages.TryRemove(id, out _))
                    {
                        removed++;
                    }
                }
            }
            else
            {
                var ids = ReceivedMessages.Values.Where(x => x.ExpiresAt < timeout).Select(x => x.DbId).ToList();
                foreach (var id in ids)
                {
                    if (PublishedMessages.TryRemove(id, out _))
                    {
                        removed++;
                    }
                }
            }
            return Task.FromResult(removed);
        }

        public Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var ret = PublishedMessages.Values
                .Where(x => x.Retries < _capOptions.Value.FailedRetryCount
                            && x.Added < DateTime.Now.AddSeconds(-10)
                            && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                .Take(200)
                .Select(x => (MediumMessage)x);

            foreach (var message in ret)
            {
                message.Origin = StringSerializer.DeSerialize(message.Content);
            }

            return Task.FromResult(ret);
        }

        public Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var ret = ReceivedMessages.Values
                 .Where(x => x.Retries < _capOptions.Value.FailedRetryCount
                             && x.Added < DateTime.Now.AddSeconds(-10)
                             && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                 .Take(200)
                 .Select(x => (MediumMessage)x);

            foreach (var message in ret)
            {
                message.Origin = StringSerializer.DeSerialize(message.Content);
            }

            return Task.FromResult(ret);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new InMemoryMonitoringApi();
        }
    }
}