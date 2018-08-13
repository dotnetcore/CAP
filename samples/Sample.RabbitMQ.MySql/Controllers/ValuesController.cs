using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;

namespace Sample.RabbitMQ.MySql.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly ICapPublisher _capBus;

        public ValuesController(AppDbContext dbContext, ICapPublisher capPublisher)
        {
            _dbContext = dbContext;
            _capBus = capPublisher;
        }

        [Route("~/publish")]
        public IActionResult PublishMessage()
        {
            _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

            return Ok();
        }

        [Route("~/publish2")]
        public IActionResult PublishMessage2()
        {
            _capBus.Publish("sample.kafka.sqlserver4", DateTime.Now);

            return Ok();
        }

        [Route("~/publishWithTrans")]
        public async Task<IActionResult> PublishMessageWithTransaction()
        {
            using (var trans = await _dbContext.Database.BeginTransactionAsync())
            using (var capTrans = _capBus.CapTransaction.Begin(trans))
            {
                for (int i = 0; i < 10; i++)
                {
                    await _capBus.PublishAsync("sample.rabbitmq.mysql", DateTime.Now);
                }

                capTrans.Commit();
            }
            return Ok();
        }

        [NonAction]
        [CapSubscribe("#.rabbitmq.mysql")]
        public void ReceiveMessage(DateTime time)
        {
            Console.WriteLine("[sample.rabbitmq.mysql] message received: " + DateTime.Now + ",sent time: " + time);
        }
    }
}
