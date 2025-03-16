using Dapper;
using Dm;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;

namespace Sample.RabbitMQ.DM.Controllers
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
        public async Task<IActionResult> Start([FromServices] IBootstrapper bootstrapper)
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
        public async Task<IActionResult> WithoutTransactionAsync()
        {
            await _capBus.PublishAsync("sample.rabbitmq.dm", DateTime.Now, cancellationToken: HttpContext.RequestAborted);

            return Ok();
        }

        [Route("~/delay/{delaySeconds:int}")]
        public async Task<IActionResult> Delay(int delaySeconds)
        {
            await _capBus.PublishDelayAsync(TimeSpan.FromSeconds(delaySeconds), "sample.rabbitmq.test", $"publish time:{DateTime.Now}, delay seconds:{delaySeconds}");

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public async Task<IActionResult> AdonetWithTransaction()
        {
            using (var connection = new DmConnection(Startup.ConnectionString))
            {
                using var transaction = await connection.BeginTransactionAsync(_capBus, true);
                await connection.ExecuteAsync(@"insert into ""cap"".TEST(NAME) values('test')", transaction: transaction);
                await _capBus.PublishAsync("sample.rabbitmq.dm", DateTime.Now);
            }
            return Ok();
        }

        //[Route("~/ef/transaction")]
        //public async Task<IActionResult> EntityFrameworkWithTransaction([FromServices] AppDbContext dbContext)
        //{
        //    using (var trans = await dbContext.Database.BeginTransactionAsync(_capBus, autoCommit: false))
        //    {
        //        await dbContext.Persons.AddAsync(new Person() { Name = "ef.transaction" });
        //        await _capBus.PublishAsync("sample.rabbitmq.dm", DateTime.Now);
        //        await dbContext.SaveChangesAsync();
        //        await trans.CommitAsync();
        //    }
        //    return Ok();
        //}

        [NonAction]
        [CapSubscribe("sample.rabbitmq.dm")]
        public void Subscriber(DateTime time)
        {
            Console.WriteLine("Publishing time:" + time);
        }
    }
}
