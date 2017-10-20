using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Sample.Kafka.SqlServer.Controllers
{
    public class Person
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uname")]
        public string Name { get; set; }

        public HAHA Haha { get; set; }

        public override string ToString()
        {
            return "Name:" + Name + ";Id:" + Id + "Haha:" + Haha?.ToString();
        }
    }

    public class HAHA
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uname")]
        public string Name { get; set; }
        public override string ToString()
        {
            return "Name:" + Name + ";Id:" + Id;
        }
    }


    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        private readonly ICapPublisher _capBus;
        private readonly AppDbContext _dbContext;

        public ValuesController(ICapPublisher producer, AppDbContext dbContext)
        {
            _capBus = producer;
            _dbContext = dbContext;
        }


        [Route("~/publish")]
        public IActionResult PublishMessage()
        {
            var p = new Person
            {
                Id = Guid.NewGuid().ToString(),
                Name = "杨晓东",
                Haha = new HAHA
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "1-1杨晓东",
                }
            };

            _capBus.Publish("wl.yxd.test", p, "wl.yxd.test.callback");


            //_capBus.Publish("wl.cj.test", p);
            return Ok();
        }

        [CapSubscribe("wl.yxd.test.callback")]
        public void KafkaTestCallback(Person p)
        {
            Console.WriteLine("回调内容：" + p);
        }


        [CapSubscribe("wl.cj.test")]
        public string KafkaTestReceived(Person person)
        {
            Console.WriteLine(person);
            Debug.WriteLine(person);
            return "this is callback message";
        }

        [Route("~/publishWithTrans")]
        public async Task<IActionResult> PublishMessageWithTransaction()
        {
            using (var trans = await _dbContext.Database.BeginTransactionAsync())
            {
                await _capBus.PublishAsync("sample.rabbitmq.mysql", "");

                trans.Commit();
            }
            return Ok();
        }

        [CapSubscribe("sample.rabbitmq.mysql33333", Group = "Test.Group")]
        public void KafkaTest22(Person person)
        {
            var aa = _dbContext.Database;

            _dbContext.Dispose();

            Console.WriteLine("[sample.kafka.sqlserver] message received   " + person.ToString());
            Debug.WriteLine("[sample.kafka.sqlserver] message received   " + person.ToString());
        }

        //[CapSubscribe("sample.rabbitmq.mysql22222")]
        //public void KafkaTest22(DateTime time)
        //{
        //    Console.WriteLine("[sample.kafka.sqlserver] message received   " + time.ToString());
        //    Debug.WriteLine("[sample.kafka.sqlserver] message received   " + time.ToString());
        //}

        [CapSubscribe("sample.rabbitmq.mysql22222")]
        public async Task<DateTime> KafkaTest33(DateTime time)
        {
            Console.WriteLine("[sample.kafka.sqlserver] message received   " + time.ToString());
            Debug.WriteLine("[sample.kafka.sqlserver] message received   " + time.ToString());
            return await Task.FromResult(time);
        }

        [NonAction]
        [CapSubscribe("sample.kafka.sqlserver3")]
        [CapSubscribe("sample.kafka.sqlserver4")]
        public void KafkaTest()
        {
            Console.WriteLine("[sample.kafka.sqlserver] message received");
            Debug.WriteLine("[sample.kafka.sqlserver] message received");
        }
    }
}