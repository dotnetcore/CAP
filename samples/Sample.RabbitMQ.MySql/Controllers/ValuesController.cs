using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Sample.RabbitMQ.MySql.Controllers
{

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICapPublisher _capBus;

        public ValuesController(ICapPublisher capPublisher)
        {
            _capBus = capPublisher;
        }

        [Route("~/control/start")]
        public async Task<IActionResult> Start([FromServices]IBootstrapper bootstrapper)
        {
            await bootstrapper.BootstrapAsync();
            return Ok();
        }

        [Route("~/control/stop")]
        public async Task<IActionResult> Stop([FromServices] IBootstrapper bootstrapper)
        {
            await bootstrapper.DisposeAsync();
            return Ok();
        }

        [Route("~/without/transaction")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.rabbitmq.test", DateTime.Now);

            return Ok();
        }

        [Route("~/delay/{delaySeconds:int}")]
        public async Task<IActionResult> Delay(int delaySeconds)
        {
            await _capBus.PublishDelayAsync(TimeSpan.FromSeconds(delaySeconds), "sample.rabbitmq.test", $"publish time:{DateTime.Now}, delay seconds:{delaySeconds}");

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new MySqlConnection(AppDbContext.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, true))
                {
                    connection.Execute("insert into test(name) values('test')", transaction: (IDbTransaction)transaction.DbTransaction);

                    _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);
                }
            }

            return Ok();
        }

        [Route("~/ef/transaction")]
        public IActionResult EntityFrameworkWithTransaction([FromServices] AppDbContext dbContext)
        {
            using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: false))
            {
                dbContext.Persons.Add(new Person() { Name = "ef.transaction" });

                for (int i = 0; i < 1; i++)
                {
                    _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);
                }

                dbContext.SaveChanges();

                trans.Commit();
            }
            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.test")]
        public void Subscriber(string content)
        {
            Console.WriteLine($"Consume time: {DateTime.Now} \r\n   --> " +content);
        }
    }
}
