using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Sample.RabbitMQ.MongoDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IMongoClient _client;
        private readonly ICapPublisher _capBus;

        public ValuesController(IMongoClient client, ICapPublisher capBus)
        {
            _client = client;
            _capBus = capBus;
        }

        [Route("~/without/transaction")]
        public IActionResult WithoutTransaction()
        {
            _capBus.PublishAsync("sample.rabbitmq.mongodb", DateTime.Now);

            return Ok();
        }


        [Route("~/delay/{delaySeconds:int}")]
        public async Task<IActionResult> Delay(int delaySeconds)
        {
            await _capBus.PublishDelayAsync(TimeSpan.FromSeconds(delaySeconds), "sample.rabbitmq.mongodb", DateTime.Now);

            return Ok();
        }

        [Route("~/transaction/not/autocommit")]
        public IActionResult PublishNotAutoCommit()
        {
            //NOTE: before your test, your need to create database and collection at first
            //注意：MongoDB 不能在事务中创建数据库和集合，所以你需要单独创建它们，模拟一条记录插入则会自动创建
            //var mycollection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
            //mycollection.InsertOne(new BsonDocument { { "test", "test" } });

            using (var session = _client.StartTransaction(_capBus, autoCommit: false))
            {
                var collection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
                collection.InsertOne(session, new BsonDocument { { "hello", "world" } });

                _capBus.Publish("sample.rabbitmq.mongodb", DateTime.Now);

                session.CommitTransaction();
            }
            return Ok();
        }

        [Route("~/transaction/autocommit")]
        public IActionResult PublishWithoutTrans()
        {
            //NOTE: before your test, your need to create database and collection at first
            //注意：MongoDB 不能在事务中创建数据库和集合，所以你需要单独创建它们，模拟一条记录插入则会自动创建
            //var mycollection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
            //mycollection.InsertOne(new BsonDocument { { "test", "test" } });

            using (var session = _client.StartTransaction(_capBus, autoCommit: true))
            {
                var collection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
                collection.InsertOne(session, new BsonDocument { { "hello", "world" } });

                _capBus.Publish("sample.rabbitmq.mongodb", DateTime.Now);
            }

            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.mongodb")]
        public void ReceiveMessage(DateTime time)
        {
            Console.WriteLine($@"{DateTime.Now}, Subscriber invoked, Sent time:{time}");
        }
    }
}
