using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Kafka.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, IConsumerService
    {
        private readonly ICapProducerService _producer;

        public ValuesController(ICapProducerService producer)
        {
            _producer = producer;
        }

        [Route("/")]
        public IActionResult Index()
        {
            return Ok();
        }
        public string ServerPath => ((IHostingEnvironment)HttpContext.RequestServices.GetService(typeof(IHostingEnvironment))).ContentRootPath;

        [KafkaTopic("zzwl.topic.finace.callBack", Group = "test")]
        public void KafkaTest(Person person)
        {
            Console.WriteLine(person.Name);
            Console.WriteLine(person.Age);
            
        }

        [Route("~/send")]
        public async Task<IActionResult> SendTopic()
        {
            await _producer.SendAsync("zzwl.topic.finace.callBack", new Person { Name = "Test", Age = 11 });
            return Ok();
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}