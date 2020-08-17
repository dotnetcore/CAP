using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Sample.RabbitMQ.Oracle.Controllers
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
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.rabbitmq.oracle", new Person()
            {
                Id = 123,
                Name = "Bar"
            });

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new OracleConnection(AppDbContext.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, true))
                {
                    //your business code
                    connection.Execute("insert into test(name) values('test')", transaction: (IDbTransaction)transaction.DbTransaction);

                    _capBus.Publish("sample.rabbitmq.oracle", DateTime.Now);
                }
            }

            return Ok();
        }

        [Route("~/ef/transaction")]
        public IActionResult EntityFrameworkWithTransaction([FromServices] AppDbContext dbContext)
        {
            using (dbContext.Database.BeginTransaction(_capBus, autoCommit: true))
            {
                dbContext.Persons.Add(new Person() { Name = "ef.transaction" });

                _capBus.Publish("sample.rabbitmq.oracle", DateTime.Now);
            }
            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.oracle")]
        public void Subscriber(DateTime p)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.oracle", Group = "group.test2")]
        public void Subscriber2(DateTime p, [FromCap] CapHeader header)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }
    }
}
