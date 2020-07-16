using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Kafka.InMemory.Controllers
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
            await _capBus.PublishAsync("sample.azure.mysql2", DateTime.Now);

            return Ok();
        }

        [CapSubscribe("sample.azure.mysql2")]
        public void Test2T2(DateTime value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }
    }
}