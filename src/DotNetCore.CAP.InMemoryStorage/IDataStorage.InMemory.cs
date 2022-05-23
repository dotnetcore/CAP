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
        private readonly ISerializer _serializer;

        public InMemoryStorage(IOptions<CapOptions> capOptions, ISerializer serializer)
        {
            _capOptions = capOptions;
            _serializer = serializer;
        }

        public static ConcurrentDictionary<string, MemoryMessage> PublishedMessages { get; } = new();

        public static ConcurrentDictionary<string, MemoryMessage> ReceivedMessages { get; } = new();

        public Task ChangePublishStateAsync(MediumMessage message, StatusName state)
        {
            PublishedMessages[message.DbId].StatusName = state;
            PublishedMessages[message.DbId].ExpiresAt = message.ExpiresAt;
            PublishedMessages[message.DbId].Content = _serializer.Serialize(message.Origin);
            return Task.CompletedTask;
        }

        public Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            ReceivedMessages[message.DbId].StatusName = state;
            ReceivedMessages[message.DbId].ExpiresAt = message.ExpiresAt;
            ReceivedMessages[message.DbId].Content = _serializer.Serialize(message.Origin);
            return Task.CompletedTask;
        }

        public MediumMessage StoreMessage(string name, Message content, object? dbTransaction = null)
        {
            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = _serializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            PublishedMessages[message.DbId] = new MemoryMessage
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
                Origin = null!,
                Name = name,
                Content = content,
                Retries = _capOptions.Value.FailedRetryCount,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddSeconds(_capOptions.Value.FailedMessageExpiredAfter),
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
                Content = _serializer.Serialize(mdMessage.Origin),
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
                var ids = PublishedMessages.Values
                    .Where(x => x.ExpiresAt < timeout)
                    .Select(x => x.DbId)
                    .Take(batchCount);

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
                var ids = ReceivedMessages.Values
                    .Where(x => x.ExpiresAt < timeout)
                    .Select(x => x.DbId)
                    .Take(batchCount);

                foreach (var id in ids)
                {
                    if (ReceivedMessages.TryRemove(id, out _))
                    {
                        removed++;
                    }
                }
            }

            return Task.FromResult(removed);
        }

        public Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            IEnumerable<MediumMessage> result = PublishedMessages.Values
                .Where(x => x.Retries < _capOptions.Value.FailedRetryCount
                            && x.Added < DateTime.Now.AddSeconds(-10)
                            && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                .Take(200)
                .Select(x => (MediumMessage)x).ToList();

            foreach (var message in result)
            {
                message.Origin = _serializer.Deserialize(message.Content)!;
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            IEnumerable<MediumMessage> result = ReceivedMessages.Values
                .Where(x => x.Retries < _capOptions.Value.FailedRetryCount
                            && x.Added < DateTime.Now.AddSeconds(-10)
                            && (x.StatusName == StatusName.Scheduled || x.StatusName == StatusName.Failed))
                .Take(200)
                .Select(x => (MediumMessage)x).ToList();

            foreach (var message in result)
            {
                message.Origin = _serializer.Deserialize(message.Content)!;
            }

            return Task.FromResult(result);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new InMemoryMonitoringApi();
        }
    }
}