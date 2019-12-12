using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Sample.Kafka.PostgreSql.Controllers
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
            await _capBus.PublishAsync("sample.kafka.postgrsql", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new NpgsqlConnection(""))
            {
                using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
                {
                    //your business code
                    connection.Execute("insert into test(name) values('test')", transaction: (IDbTransaction)transaction.DbTransaction);

                    _capBus.Publish("sample.kafka.postgrsql", DateTime.Now);

                    transaction.Commit();
                }
            }

            return Ok();
        }


        [CapSubscribe("sample.kafka.postgrsql")]
        public void Test2(DateTime value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }
    }
}