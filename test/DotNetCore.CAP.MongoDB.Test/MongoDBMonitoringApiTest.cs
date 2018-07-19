using MongoDB.Driver;
using DotNetCore.CAP.MongoDB;
using Xunit;
using System;
using DotNetCore.CAP.Models;
using FluentAssertions;

namespace DotNetCore.CAP.MongoDB.Test
{
    public class MongoDBMonitoringApiTest
    {
        private MongoClient _client;
        private MongoDBOptions _options;
        private MongoDBMonitoringApi _api;

        public MongoDBMonitoringApiTest()
        {
            _client = new MongoClient(ConnectionUtil.ConnectionString);
            _options = new MongoDBOptions();
            _api = new MongoDBMonitoringApi(_client, _options);

            Init();
        }

        private void Init()
        {
            var helper = new MongoDBUtil();
            var database = _client.GetDatabase(_options.Database);
            var collection = database.GetCollection<CapPublishedMessage>(_options.Published);
            collection.InsertMany(new CapPublishedMessage[]
            {
                new CapPublishedMessage
                {
                    Id = helper.GetNextSequenceValue(database,_options.Published),
                    Added = DateTime.Now.AddHours(-1),
                    StatusName = "Failed"
                },
                new CapPublishedMessage
                {
                    Id = helper.GetNextSequenceValue(database,_options.Published),
                    Added = DateTime.Now,
                    StatusName = "Failed"
                }
            });
        }

        [Fact]
        public void HourlyFailedJobs_Test()
        {
            var result = _api.HourlyFailedJobs(MessageType.Publish);
            result.Should().HaveCount(24);
        }
    }
}