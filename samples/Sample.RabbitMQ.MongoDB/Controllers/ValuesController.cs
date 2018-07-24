using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.MongoDB;
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
        private readonly IMongoTransaction _mongoTransaction;

        public ValuesController(IMongoClient client, ICapPublisher capPublisher, IMongoTransaction mongoTransaction)
        {
            _client = client;
            _capPublisher = capPublisher;
            _mongoTransaction = mongoTransaction;
        }

        [Route("~/publish")]
        public async Task<IActionResult> PublishWithTrans()
        {
            using (var trans = await _mongoTransaction.BegeinAsync())
            {
                var collection = _client.GetDatabase("TEST").GetCollection<BsonDocument>("test");
                collection.InsertOne(trans.GetSession(), new BsonDocument { { "hello", "world" } });

                await _capPublisher.PublishWithMongoAsync("sample.rabbitmq.mongodb", DateTime.Now, trans);
            }
            return Ok();
        }

        [Route("~/publish/not/autocommit")]
        public IActionResult PublishNotAutoCommit()
        {
            using (var trans = _mongoTransaction.Begein(autoCommit: false))
            {
                var session = trans.GetSession();

                var collection = _client.GetDatabase("TEST").GetCollection<BsonDocument>("test");
                collection.InsertOne(session, new BsonDocument { { "Hello", "World" } });

                _capPublisher.PublishWithMongo("sample.rabbitmq.mongodb", DateTime.Now, trans);

                //Do something, and commit by yourself.
                session.CommitTransaction();
            }
            return Ok();
        }

        [Route("~/publish/rollback")]
        public IActionResult PublishRollback()
        {
            using (var trans = _mongoTransaction.Begein(autoCommit: false))
            {
                var session = trans.GetSession();
                try
                {
                    _capPublisher.PublishWithMongo("sample.rabbitmq.mongodb", DateTime.Now, trans);
                    //Do something, but
                    throw new Exception("Foo");
                    session.CommitTransaction();
                }
                catch (System.Exception ex)
                {
                    session.AbortTransaction();
                    return StatusCode(500, ex.Message);
                }
            }
        }

        [Route("~/publish/without/trans")]
        public IActionResult PublishWithoutTrans()
        {
            _capPublisher.PublishWithMongo("sample.rabbitmq.mongodb", DateTime.Now);
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
