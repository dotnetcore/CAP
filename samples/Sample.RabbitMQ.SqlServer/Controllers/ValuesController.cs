using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sample.RabbitMQ.SqlServer.Messages;

namespace Sample.RabbitMQ.SqlServer.Controllers
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
            await _capBus.PublishAsync("sample.rabbitmq.sqlserver", new Person()
            {
                Id = 123,
                Name = "Bar"
            });

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new SqlConnection(AppDbContext.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, true))
                {
                    //your business code
                    connection.Execute("insert into test(name) values('test')", transaction: transaction);

                    _capBus.Publish("sample.rabbitmq.sqlserver", new Person()
                    {
                        Id = 123,
                        Name = "Bar"
                    });
                }
            }

            return Ok();
        }

        [Route("~/ef/transaction")]
        public IActionResult EntityFrameworkWithTransaction([FromServices]AppDbContext dbContext)
        {
            using (dbContext.Database.BeginTransaction(_capBus, autoCommit: true))
            {
                dbContext.Persons.Add(new Person() { Name = "ef.transaction" });
                dbContext.SaveChanges();
                _capBus.Publish("sample.rabbitmq.sqlserver", new Person()
                {
                    Id = 123,
                    Name = "Bar"
                });
            }
            return Ok();
        }

        [Route("~/type/publish")]
        public async Task<IActionResult> TypePublish()
        {
            await using (var connection = new SqlConnection(AppDbContext.ConnectionString))
            {
                using var transaction = connection.BeginTransaction(_capBus);
                // This is where you would do other work that is going to persist data to your database

                var message = TestMessage.Create($"This is message text created at {DateTime.Now:O}.");

                await _capBus.PublishAsync(typeof(TestMessage).FullName, message);
                transaction.Commit();
            }

            return Content("ok");
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.sqlserver")]
        public void Subscriber(Person p)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.sqlserver", Group = "group.test2")]
        public void Subscriber2(Person p, [FromCap]CapHeader header)
        {
            var id = header[Headers.MessageId];

            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }
    }
}
