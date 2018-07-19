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
            var groupby = new BsonDocument {
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
            };

            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);

            var result =
            collection.Aggregate()
            .Match(x => x.Added > DateTime.UtcNow.AddHours(-24))
            .Group(groupby)
            .ToList();

            var endDate = DateTime.UtcNow;
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

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            throw new NotImplementedException();
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            throw new NotImplementedException();
        }

        public int PublishedFailedCount()
        {
            throw new NotImplementedException();
        }

        public int PublishedSucceededCount()
        {
            throw new NotImplementedException();
        }

        public int ReceivedFailedCount()
        {
            throw new NotImplementedException();
        }

        public int ReceivedSucceededCount()
        {
            throw new NotImplementedException();
        }
    }
}