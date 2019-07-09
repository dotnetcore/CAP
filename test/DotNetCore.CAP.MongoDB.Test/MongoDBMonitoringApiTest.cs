using System;
using System.Linq;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using FluentAssertions;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    [Collection("MongoDB")]
    public class MongoDBMonitoringApiTest : DatabaseTestHost
    {
        private readonly MongoDBMonitoringApi _api;

        public MongoDBMonitoringApiTest()
        {
            _api = new MongoDBMonitoringApi(MongoClient, MongoDBOptions);

            var collection = Database.GetCollection<PublishedMessage>(MongoDBOptions.Value.PublishedCollection);
            collection.InsertMany(new[]
            {
                new PublishedMessage
                {
                    Id = SnowflakeId.Default().NextId(),
                    Added = DateTime.Now.AddHours(-1),
                    StatusName = "Failed",
                    Version = "v1",
                    Content = "abc"
                },
                new PublishedMessage
                {
                    Id =  SnowflakeId.Default().NextId(),
                    Added = DateTime.Now,
                    StatusName = "Failed",
                    Version = "v1",
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

        [Fact]
        public void PublishedFailedCount_Test()
        {
            var count = _api.PublishedFailedCount();
            count.Should().BeGreaterThan(1);
        }
    }
}