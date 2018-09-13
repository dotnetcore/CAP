using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Sample.Kafka.MySql.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        private readonly ICapPublisher _capBus;

        public ValuesController(ICapPublisher producer)
        {
            _capBus = producer;
        }

        [Route("~/without/transaction")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.rabbitmq.mysql", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new MySqlConnection(""))
            {
                using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
                {
                    //your business code
                    connection.Execute("insert into test(name) values('test')", transaction: (IDbTransaction)transaction.DbTransaction);

                    for (int i = 0; i < 5; i++)
                    {
                        _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);
                    }

                    transaction.Commit();
                }
            }

            return Ok();
        }


        [CapSubscribe("#.test2")]
        public void Test2(int value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }
    }
}