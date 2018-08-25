using System;
using System.Data.SqlClient;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Kafka.SqlServer.Controllers
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
            _capBus.Publish("sample.kafka.sqlserver", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new SqlConnection(Startup.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
                {
                    //your business code
                    connection.Execute("insert into dbo.test1(tname) values('test');", transaction: transaction);

                    _capBus.Publish("sample.kafka.sqlserver", DateTime.Now);

                    transaction.Commit();
                }
            }

            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.kafka.sqlserver")]
        public void Subscriber(DateTime time)
        {
            Console.WriteLine($@"{DateTime.Now}, Subscriber invoked, Sent time:{time}");
        }
    }
}