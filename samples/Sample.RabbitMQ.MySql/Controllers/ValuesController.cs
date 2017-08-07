using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        [CapSubscribe("sample.rabbitmq.mysql")]
        public void ReceiveMessage()
        {
            Console.WriteLine("[sample.rabbitmq.mysql] message received");
            Debug.WriteLine("[sample.rabbitmq.mysql] message received");
        }
    }
}
