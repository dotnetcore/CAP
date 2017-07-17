using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.RabbitMQ;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Kafka.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        private readonly ICapPublisher _producer;
        private readonly AppDbContext _dbContext ;

        public ValuesController(ICapPublisher producer, AppDbContext dbContext)
        {
            _producer = producer;
            _dbContext = dbContext;
        }

        [Route("/")]
        public IActionResult Index()
        {
            return Ok();
        }
        public string ServerPath => ((IHostingEnvironment)HttpContext.RequestServices.GetService(typeof(IHostingEnvironment))).ContentRootPath;

        [CapSubscribe("zzwl.topic.finace.callBack", Group = "test")]
        public void KafkaTest(Person person)
        {
            Console.WriteLine(DateTime.Now);
        }

        [Route("~/send")]
        public async Task<IActionResult> SendTopic()
        {
            using (var trans = _dbContext.Database.BeginTransaction())
            {
                await _producer.PublishAsync("zzwl.topic.finace.callBack","");

                trans.Commit();
            }

            return Ok();
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}