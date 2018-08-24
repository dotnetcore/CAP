using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
//using MySql.Data.MySqlClient;

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
            using (var connection = new SqlConnection(Startup.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
                {
                    //your business code
                    connection.Execute("insert into dbo.test1(tname) values('test');", transaction: (IDbTransaction)transaction.DbTransaction);

                    _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

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