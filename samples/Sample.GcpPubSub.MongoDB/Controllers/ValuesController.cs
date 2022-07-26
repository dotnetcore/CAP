using System;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Sample.GcpPubSub.GoogleSpanner
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        public class MyObj
        {
            public string SingerId { get; set; }
            public string FirstName { get; set; }
        }

        private readonly IMongoClient _client;
        private readonly ICapPublisher _capBus;

        public ValuesController(IMongoClient client, ICapPublisher producer)
        {
            _capBus = producer;
            _client = client;
        }

        [Route("~/without/transaction")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.gcppubsub.googlespanner", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            var random = new Random().Next(1, 10000).ToString();
            try
            {
                //NOTE: before your test, create "test" database and "test.collection" collection first
                //The MongoDB should have replication enabled.

                using (var session = _client.StartTransaction(_capBus, autoCommit: false))
                {
                    var collection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
                    collection.InsertOne(session, new BsonDocument { 
                        { "SingerId", random },
                        { "FirstName", "John"+random },
                        { "LastName", "Doe"+random }                    
                    });

                    var msg = new MyObj { SingerId = random, FirstName = "John" + random };
                    _capBus.Publish("sample.gcppubsub.mongodb", msg);

                    session.CommitTransaction();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Ok();
        }


        [CapSubscribe("sample.gcppubsub.mongodb")]
        public void Test2(MyObj value)
        {
            Console.WriteLine("Subscriber output message: " + JsonSerializer.Serialize(value));
        }
    }
}