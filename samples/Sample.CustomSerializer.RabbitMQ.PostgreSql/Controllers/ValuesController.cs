using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Sample.CustomSerializer.Rabbit.PostgreSql.Domain;

namespace Sample.Kafka.PostgreSql.Controllers
{
    [Route("api")]
    public class ValuesController : Controller, ICapSubscribe
    {
        private readonly ICapPublisher _capBus;
        private readonly IPublisher<HanoiDto> _publisher;
        public ValuesController(ICapPublisher producer, IPublisher<HanoiDto> publisher)
        {
            _capBus = producer;
            _publisher = publisher;
        }

        [Route("pub")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.kafka.postgrsql", DateTime.Now);

            return Ok();
        }

        [Route("hanoi")]
        public async Task<IActionResult> Hanoi()
        {
            await _publisher.PublishAsync("hanoi", new HanoiDto() { Name = "hanoi", Code = 10000 });

            return Ok();
        }

        [CapSubscribe("hanoi")]
        public void Hanoi(HanoiDto value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }


        [CapSubscribe("sample.kafka.postgrsql")]
        public void Test2(DateTime value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }


    }
}