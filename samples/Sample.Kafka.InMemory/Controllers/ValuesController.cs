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
            await _capBus.PublishAsync("persistent://public/default/supermatelsotoppic", DateTime.Now);

            return Ok();
        }

        [CapSubscribe("persistent://public/default/supermatelsotoppic")]
        public void Test2T2(string value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }
    }
}