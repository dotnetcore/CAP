// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBMonitoringApi : IMonitoringApi
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDBOptions _options;

        public MongoDBMonitoringApi(IMongoClient client, IOptions<MongoDBOptions> options)
        {
            var mongoClient = client ?? throw new ArgumentNullException(nameof(client));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            _database = mongoClient.GetDatabase(_options.DatabaseName);
        }

        public async Task<MediumMessage?> GetPublishedMessageAsync(long id)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);
            var message = await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return new MediumMessage
            {
                Added = message.Added,
                Content = message.Content,
                DbId = message.Id.ToString(),
                ExpiresAt = message.ExpiresAt,
                Retries = message.Retries
            };
        }

        public async Task<MediumMessage?> GetReceivedMessageAsync(long id)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);
            var message = await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return new MediumMessage
            {
                Added = message.Added,
                Content = message.Content,
                DbId = message.Id.ToString(),
                ExpiresAt = message.ExpiresAt,
                Retries = message.Retries
            };
        }

        public StatisticsDto GetStatistics()
        {
            var publishedCollection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);
            var receivedCollection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);

            var statistics = new StatisticsDto
            {
                PublishedSucceeded =
                    (int)publishedCollection.CountDocuments(x => x.StatusName == nameof(StatusName.Succeeded)),
                PublishedFailed =
                    (int)publishedCollection.CountDocuments(x => x.StatusName == nameof(StatusName.Failed)),
                ReceivedSucceeded =
                    (int)receivedCollection.CountDocuments(x => x.StatusName == nameof(StatusName.Succeeded)),
                ReceivedFailed = (int)receivedCollection.CountDocuments(x => x.StatusName == nameof(StatusName.Failed))
            };
            return statistics;
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            return GetHourlyTimelineStats(type, nameof(StatusName.Failed));
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            return GetHourlyTimelineStats(type, nameof(StatusName.Succeeded));
        }

        public PagedQueryResult<MessageDto> Messages(MessageQueryDto queryDto)
        {
            return queryDto.MessageType == MessageType.Publish
                       ? FindPublishedMessages(queryDto)
                       : FindReceivedMessages(queryDto);
        }

        private PagedQueryResult<MessageDto> FindReceivedMessages(MessageQueryDto queryDto)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);
            var builder = Builders<ReceivedMessage>.Filter;
            var filter = builder.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
                filter &= builder.Regex(x => x.StatusName, $"/{queryDto.StatusName}/i");

            if (!string.IsNullOrEmpty(queryDto.Name)) filter &= builder.Eq(x => x.Name, queryDto.Name);

            if (!string.IsNullOrEmpty(queryDto.Group)) filter &= builder.Eq(x => x.Group, queryDto.Group);

            if (!string.IsNullOrEmpty(queryDto.Content))
                filter &= builder.Regex(x => x.Content, $".*{queryDto.Content}.*");

            var items = collection.Find(filter)
                                  .SortByDescending(x => x.Added)
                                  .Skip(queryDto.PageSize * queryDto.CurrentPage)
                                  .Limit(queryDto.PageSize)
                                  .ToList()
                                  .Select(x => new MessageDto
                                               {
                                                   Id = x.Id.ToString(),
                                                   Version = x.Version.ToString(),
                                                   Group = x.Group,
                                                   Name = x.Name,
                                                   Content = x.Content,
                                                   Added = x.Added.ToLocalTime(),
                                                   ExpiresAt = x.ExpiresAt?.ToLocalTime(),
                                                   Retries = x.Retries,
                                                   StatusName = x.StatusName
                                               })
                                  .ToList();

            var count = collection.CountDocuments(filter);

            return new PagedQueryResult<MessageDto> { Items = items, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
        }

        private PagedQueryResult<MessageDto> FindPublishedMessages(MessageQueryDto queryDto)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);

            var builder = Builders<PublishedMessage>.Filter;
            var filter = builder.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
                filter &= builder.Regex(x => x.StatusName, $"/{queryDto.StatusName}/i");

            if (!string.IsNullOrEmpty(queryDto.Name)) filter &= builder.Eq(x => x.Name, queryDto.Name);

            if (!string.IsNullOrEmpty(queryDto.Content))
                filter &= builder.Regex(x => x.Content, $".*{queryDto.Content}.*");

            var items = collection.Find(filter)
                                  .SortByDescending(x => x.Added)
                                  .Skip(queryDto.PageSize * queryDto.CurrentPage)
                                  .Limit(queryDto.PageSize)
                                  .ToList()
                                  .Select(x => new MessageDto
                                               {
                                                   Id = x.Id.ToString(),
                                                   Version = x.Version.ToString(),
                                                   Group = null,
                                                   Name = x.Name,
                                                   Content = x.Content,
                                                   Added = x.Added.ToLocalTime(),
                                                   ExpiresAt = x.ExpiresAt?.ToLocalTime(),
                                                   Retries = x.Retries,
                                                   StatusName = x.StatusName
                                               })
                                  .ToList();

            var count = collection.CountDocuments(filter);

            return new PagedQueryResult<MessageDto> { Items = items, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
        }

        public int PublishedFailedCount()
        {
            return GetNumberOfMessage(_options.PublishedCollection, nameof(StatusName.Failed));
        }

        public int PublishedSucceededCount()
        {
            return GetNumberOfMessage(_options.PublishedCollection, nameof(StatusName.Succeeded));
        }

        public int ReceivedFailedCount()
        {
            return GetNumberOfMessage(_options.ReceivedCollection, nameof(StatusName.Failed));
        }

        public int ReceivedSucceededCount()
        {
            return GetNumberOfMessage(_options.ReceivedCollection, nameof(StatusName.Succeeded));
        }

        private int GetNumberOfMessage(string collectionName, string statusName)
        {
            return collectionName.Equals(_options.PublishedCollection, StringComparison.InvariantCultureIgnoreCase)
                       ? GetNumberOfPublishedMessages(statusName)
                       : GetNumberOfReceivedMessages(statusName);
        }

        private int GetNumberOfReceivedMessages(string statusName)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);
            var filter = Builders<ReceivedMessage>.Filter.Eq(x => x.StatusName, statusName);
            var count = collection.CountDocuments(filter);
            return int.Parse(count.ToString());
        }

        private int GetNumberOfPublishedMessages(string statusName)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);
            var filter = Builders<PublishedMessage>.Filter.Eq(x => x.StatusName, statusName);
            var count = collection.CountDocuments(filter);
            return int.Parse(count.ToString());
        }

        private IDictionary<DateTime, int> GetHourlyTimelineStats(MessageType type, string statusName)
        {
            var endDate = DateTime.UtcNow;
            var published = _database.GetCollection<PublishedMessage>(_options.PublishedCollection).AsQueryable();
            var received = _database.GetCollection<PublishedMessage>(_options.PublishedCollection).AsQueryable();

            var result = type == MessageType.Publish 
                             ? published.Where(x => x.Added > endDate.AddHours(-24) && x.StatusName == statusName)
                                        .GroupBy(x => new
                                                      {
                                                          x.Added.Year,
                                                          x.Added.Month,
                                                          x.Added.Day,
                                                          x.Added.Hour
                                                      })
                                        .Select(kv => new { kv.Key, Count = kv.Count() })
                                        .ToList()
                             : received.Where(x => x.Added > endDate.AddHours(-24) && x.StatusName == statusName)
                                       .GroupBy(x => new
                                                     {
                                                         x.Added.Year,
                                                         x.Added.Month,
                                                         x.Added.Day,
                                                         x.Added.Hour
                                                     })
                                       .Select(kv => new { kv.Key, Count = kv.Count() })
                                       .ToList();

            var dic = new Dictionary<DateTime, int>();
            for (var i = 0; i < 24; i++)
            {
                dic.Add(DateTime.Parse(endDate.ToLocalTime().ToString("yyyy-MM-dd HH:00:00")), 0);
                endDate = endDate.AddHours(-1);
            }

            result.ForEach(d =>
            {
                var dateTime = new DateTime(d.Key.Year, d.Key.Month, d.Key.Day, d.Key.Hour, 0, 0);
                dic[dateTime.ToLocalTime()] = d.Count;
            });

            return dic;
        }
    }
}