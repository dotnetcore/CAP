using System;
using System.Collections.Generic;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    internal class MongoDBMonitoringApi : IMonitoringApi
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
            throw new NotImplementedException();
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