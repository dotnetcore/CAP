using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Kafka.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        private readonly ICapPublisher _capBus;
        private readonly AppDbContext _dbContext;

        public ValuesController(ICapPublisher producer, AppDbContext dbContext)
        {
            _capBus = producer;
            _dbContext = dbContext;
        }

        [Route("~/publish")]
        public IActionResult PublishMessage()
        {
            _capBus.Publish("sample.rabbitmq.mysql", "");
            return Ok();
        }

        [Route("~/publishWithTrans")]
        public async Task<IActionResult> PublishMessageWithTransaction()
        {
            using (var trans = await _dbContext.Database.BeginTransactionAsync())
            {
                await _capBus.PublishAsync("sample.rabbitmq.mysql", "");
                trans.Commit();
            }
            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.kafka.sqlserver", Group = "test")]
        public void KafkaTest()
        {
            Console.WriteLine("[sample.kafka.sqlserver] message received");
            Debug.WriteLine("[sample.kafka.sqlserver] message received");
        }
    }
}