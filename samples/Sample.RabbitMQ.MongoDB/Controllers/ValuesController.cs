using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ICapPublisher _capPublisher;

        public ValuesController(IMongoClient client, ICapPublisher capPublisher)
        {
            _client = client;
            _capPublisher = capPublisher;
        }

        [Route("~/publish")]
        public IActionResult PublishWithSession()
        {
            using (var session = _client.StartSession())
            {
                session.StartTransaction();
                var collection = _client.GetDatabase("TEST").GetCollection<BsonDocument>("test");
                collection.InsertOne(session, new BsonDocument { { "hello", "world" } });

                _capPublisher.PublishWithMongoSession("sample.rabbitmq.mongodb", DateTime.Now, session);

                session.CommitTransaction();
            }
            return Ok();
        }

        [Route("~/publish_rollback")]
        public IActionResult PublishRollback()
        {
            using (var session = _client.StartSession())
            {
                try
                {
                    session.StartTransaction();
                    _capPublisher.PublishWithMongoSession("sample.rabbitmq.mongodb", DateTime.Now, session);
                    throw new Exception("Foo");
                }
                catch (System.Exception ex)
                {
                    session.AbortTransaction();
                    return StatusCode(500, ex.Message);
                }
            }
        }
    }
}
