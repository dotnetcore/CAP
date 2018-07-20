using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBMonitoringApi : IMonitoringApi
    {
        private IMongoClient _client;
        private MongoDBOptions _options;
        private IMongoDatabase _database;

        public MongoDBMonitoringApi(IMongoClient client, MongoDBOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _database = _client.GetDatabase(_options.Database);
        }

        public StatisticsDto GetStatistics()
        {
            var publishedCollection = _database.GetCollection<CapPublishedMessage>(_options.Published);
            var receivedCollection = _database.GetCollection<CapReceivedMessage>(_options.Received);

            var statistics = new StatisticsDto();

            {
                if (int.TryParse(publishedCollection.CountDocuments(x => x.StatusName == StatusName.Succeeded).ToString(), out var count))
                    statistics.PublishedSucceeded = count;
            }
            {
                if (int.TryParse(publishedCollection.CountDocuments(x => x.StatusName == StatusName.Failed).ToString(), out var count))
                    statistics.PublishedFailed = count;
            }
            {
                if (int.TryParse(receivedCollection.CountDocuments(x => x.StatusName == StatusName.Succeeded).ToString(), out var count))
                    statistics.ReceivedSucceeded = count;
            }
            {
                if (int.TryParse(receivedCollection.CountDocuments(x => x.StatusName == StatusName.Failed).ToString(), out var count))
                    statistics.ReceivedFailed = count;
            }

            return statistics;
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            return GetHourlyTimelineStats(type, StatusName.Failed);
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            return GetHourlyTimelineStats(type, StatusName.Succeeded);
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            queryDto.StatusName = StatusName.Standardized(queryDto.StatusName);

            var name = queryDto.MessageType == MessageType.Publish ? _options.Published : _options.Received;
            var collection = _database.GetCollection<MessageDto>(name);
            
            var builder = Builders<MessageDto>.Filter;
            var filter = builder.Empty;
            if (!string.IsNullOrEmpty(queryDto.StatusName))
            {
                filter = filter & builder.Eq(x => x.StatusName, queryDto.StatusName);
            }
            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                filter = filter & builder.Eq(x => x.Name, queryDto.Name);
            }
            if (!string.IsNullOrEmpty(queryDto.Group))
            {
                filter = filter & builder.Eq(x => x.Group, queryDto.Group);
            }
            if (!string.IsNullOrEmpty(queryDto.Content))
            {
                filter = filter & builder.Regex(x => x.Content, ".*" + queryDto.Content + ".*");
            }

            var result =
            collection.Find(filter)
            .SortByDescending(x => x.Added)
            .Skip(queryDto.PageSize * queryDto.CurrentPage)
            .Limit(queryDto.PageSize)
            .ToList();

            return result;
        }

        public int PublishedFailedCount()
        {
            return GetNumberOfMessage(_options.Published, StatusName.Failed);
        }

        public int PublishedSucceededCount()
        {
            return GetNumberOfMessage(_options.Published, StatusName.Succeeded);
        }

        public int ReceivedFailedCount()
        {
            return GetNumberOfMessage(_options.Received, StatusName.Failed);
        }

        public int ReceivedSucceededCount()
        {
            return GetNumberOfMessage(_options.Received, StatusName.Succeeded);
        }

        private int GetNumberOfMessage(string collectionName, string statusName)
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var count = collection.CountDocuments(new BsonDocument { { "StatusName", statusName } });
            return int.Parse(count.ToString());
        }

        private IDictionary<DateTime, int> GetHourlyTimelineStats(MessageType type, string statusName)
        {
            var collectionName = type == MessageType.Publish ? _options.Published : _options.Received;
            var endDate = DateTime.UtcNow;

            var groupby = new BsonDocument {
                { "$group", new BsonDocument{
                    { "_id", new BsonDocument {
                            { "Key", new BsonDocument {
                                { "$dateToString", new BsonDocument {
                                    { "format", "%Y-%m-%d %H:00:00"},
                                    { "date", "$Added"}
                                }}
                            }}
                        }
                },
                { "Count", new BsonDocument{
                    { "$sum", 1}
                }}
                }}
            };

            var match = new BsonDocument { { "$match", new BsonDocument {
                { "Added", new BsonDocument { { "$gt", endDate.AddHours(-24) } } },
                { "StatusName", new BsonDocument { { "$eq", statusName} }
                } } } };
            var pipeline = new BsonDocument[] { match, groupby };

            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var result = collection.Aggregate<BsonDocument>(pipeline: pipeline).ToList();

            var dic = new Dictionary<DateTime, int>();
            for (var i = 0; i < 24; i++)
            {
                dic.Add(DateTime.Parse(endDate.ToString("yyyy-MM-dd HH:00:00")), 0);
                endDate = endDate.AddHours(-1);
            }
            result.ForEach(d =>
            {
                var key = d["_id"].AsBsonDocument["Key"].AsString;
                if (DateTime.TryParse(key, out var dateTime))
                {
                    dic[dateTime] = d["Count"].AsInt32;
                }
            });

            return dic;
        }
    }
}