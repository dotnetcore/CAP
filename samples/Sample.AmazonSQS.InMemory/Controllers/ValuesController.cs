using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;

namespace Sample.AmazonSQS.InMemory.Controllers
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
            await _capBus.PublishAsync("sample.aws.in-memory", DateTime.Now);

            return Ok();
        }

        [CapSubscribe("sample.aws.in-memory")]
        public void SubscribeInMemoryTopic(DateTime value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }
    }
}