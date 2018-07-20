using MongoDB.Driver;
using DotNetCore.CAP.MongoDB;
using Xunit;
using System;
using DotNetCore.CAP.Models;
using FluentAssertions;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using System.Linq;

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
                    StatusName = "Failed",
                    Content = "abc"
                },
                new CapPublishedMessage
                {
                    Id = helper.GetNextSequenceValue(database,_options.Published),
                    Added = DateTime.Now,
                    StatusName = "Failed",
                    Content = "bbc"
                }
            });
        }

        [Fact]
        public void HourlyFailedJobs_Test()
        {
            var result = _api.HourlyFailedJobs(MessageType.Publish);
            result.Should().HaveCount(24);
        }

        [Fact]
        public void Messages_Test()
        {
            var messages =
            _api.Messages(new MessageQueryDto
            {
                MessageType = MessageType.Publish,
                StatusName = StatusName.Failed,
                Content = "b",
                CurrentPage = 1,
                PageSize = 1
            });

            messages.Should().HaveCount(1);
            messages.First().Content.Should().Contain("b");
        }
    }
}