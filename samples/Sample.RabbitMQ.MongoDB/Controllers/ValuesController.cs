using System;
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
        private readonly ICapPublisher _capPublisher;

        public ValuesController(IMongoClient client, ICapPublisher capPublisher)
        {
            _client = client;
            _capPublisher = capPublisher;
        }

        [Route("~/publish")]
        public IActionResult PublishWithTrans()
        {
            //var mycollection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
            //mycollection.InsertOne(new BsonDocument { { "test", "test" } });

            using (var session = _client.StartSession())
            using (var trans = _capPublisher.CapTransaction.Begin(session))
            {
                var collection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
                collection.InsertOne(session, new BsonDocument { { "hello", "world" } });

                _capPublisher.Publish("sample.rabbitmq.mongodb", DateTime.Now);

                trans.Commit();
            }
            return Ok();
        }

        [Route("~/publish/autocommit")]
        public IActionResult PublishNotAutoCommit()
        {
            using (var session = _client.StartSession())
            using (_capPublisher.CapTransaction.Begin(session, true))
            {
                var collection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
                collection.InsertOne(session, new BsonDocument { { "hello2", "world2" } });

                _capPublisher.Publish("sample.rabbitmq.mongodb", DateTime.Now);
            }

            return Ok();
        }

        [Route("~/publish/without/trans")]
        public IActionResult PublishWithoutTrans()
        {
            _capPublisher.Publish("sample.rabbitmq.mongodb", DateTime.Now);
            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.mongodb")]
        public void ReceiveMessage(DateTime time)
        {
            Console.WriteLine("[sample.rabbitmq.mongodb] message received: " + DateTime.Now + ",sent time: " + time);
        }
    }
}
