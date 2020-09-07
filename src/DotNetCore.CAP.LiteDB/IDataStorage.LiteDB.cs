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
using LiteDB;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.LiteDB
{
    internal class LiteDBStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly  LiteDBOptions  _ldboption;
        LiteDatabase _lite;
        public LiteDBStorage(IOptions<CapOptions> capOptions, IOptions<LiteDBOptions> ldboption)
        {
            _capOptions = capOptions;
            _ldboption = ldboption.Value;
            _lite = new LiteDatabase(_ldboption.ConnectionString);
            PublishedMessages = _lite.GetCollection<LiteDBMessage>(nameof(PublishedMessages));
            PublishedMessages.EnsureIndex( l => l.Id,true);
            ReceivedMessages = _lite.GetCollection<LiteDBMessage>(nameof(ReceivedMessages));
            ReceivedMessages.EnsureIndex(l => l.Id, true);
        }

        public static ILiteCollection< LiteDBMessage> PublishedMessages { get; private set; } 

        public static ILiteCollection<  LiteDBMessage> ReceivedMessages { get; private set; }  

        public Task ChangePublishStateAsync(MediumMessage message, StatusName state)
        {
            var msg = PublishedMessages.FindOne(l => l.Id == message.DbId);
            msg.StatusName = state;
            msg.ExpiresAt = message.ExpiresAt;
            PublishedMessages.Update(msg);
            return Task.CompletedTask;
        }

        public Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            var msg = ReceivedMessages.FindOne(l => l.Id == message.DbId);
            msg.StatusName = state;
            msg.ExpiresAt = message.ExpiresAt;
            ReceivedMessages.Update(msg);
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

            PublishedMessages.Insert(new LiteDBMessage()
            {
                Id = message.DbId,
                Name = name,
                Content = message.Content,
                Retries = message.Retries,
                Added = message.Added,
                ExpiresAt = message.ExpiresAt,
                StatusName = StatusName.Scheduled
            });
            return message;
        }

        public void StoreReceivedExceptionMessage(string name, string group, string content)
        {
            var id = SnowflakeId.Default().NextId().ToString();

            ReceivedMessages.Insert( new LiteDBMessage
            {
                 Id = id,
                Group = group,
                Name = name,
                Content = content,
                Retries = _capOptions.Value.FailedRetryCount,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(15),
                StatusName = StatusName.Failed
            });
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

            ReceivedMessages.Insert ( new LiteDBMessage(mdMessage.Origin)
            {
                 Id = mdMessage.DbId,
                Group = group,
                Name = name,
                Content = StringSerializer.Serialize(mdMessage.Origin),
                Retries = mdMessage.Retries,
                Added = mdMessage.Added,
                ExpiresAt = mdMessage.ExpiresAt,
                StatusName = StatusName.Scheduled
            });
            return mdMessage;
        }

        public Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            var removed = 0;
            if (table == nameof(PublishedMessages))
            {
                removed = PublishedMessages.DeleteMany(x => x.ExpiresAt < timeout);
               
            }
            else
            {
                removed = ReceivedMessages.DeleteMany(x => x.ExpiresAt < timeout);
                
            }
            return Task.FromResult(removed);
        }

        public Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var ret = PublishedMessages
                .Find(x => x.Retries < _capOptions.Value.FailedRetryCount
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
            var ret = ReceivedMessages
                 .Find(x => x.Retries < _capOptions.Value.FailedRetryCount
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
            return new LiteDBMonitoringApi();
        }
    }
}