using System;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Sample.Kafka.MySql.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICapPublisher _capBus;

        public ValuesController(ICapPublisher capPublisher)
        {
            _capBus = capPublisher;
        }

        [Route("~/without/transaction")]
        public IActionResult WithoutTransaction()
        {
            _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            //NOTE: Add `IgnoreCommandTransaction=true;` to your connection string, see https://github.com/mysql-net/MySqlConnector/issues/474
            using (var connection = new MySqlConnection(Startup.ConnectionString))
            {
                using (var transaction = connection.BeginAndJoinToTransaction(_capBus, autoCommit: false))
                {
                    //your business code
                    connection.Execute("insert into test(name) values('test')", transaction);

                    for (int i = 0; i < 5; i++)
                    {
                        _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);
                    }

                    transaction.Commit();
                }
            }

            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.mysql")]
        public void Subscriber(DateTime time)
        {
            Console.WriteLine($@"{DateTime.Now}, Subscriber invoked, Sent time:{time}");
        }
    }
}