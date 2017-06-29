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

        [KafkaTopic("zzwl.topic.finace.callBack", IsOneWay = true, GroupOrExchange = "test")]
        [NonAction]
        public void KafkaTest()
        {
            Console.WriteLine("kafka test invoked");
        }

        [Route("~/send")]
        public async Task<IActionResult> SendTopic()
        {
            await _producer.SendAsync("zzwl.topic.finace.callBack", "{\"msgBody\":\"{\\\"dealno\\\":null,\\\"businesstype\\\":\\\"1\\\",\\\"serialno\\\":\\\"435ldfhj345\\\",\\\"bankno\\\":\\\"650001\\\",\\\"amt\\\":20.0,\\\"virtualstatus\\\":1,\\\"paystatus\\\":1}\",\"callbackTopicName\":\"zzwl.topic.finace.callBack\",\"createId\":null,\"retryLimit\":0}");
            return Ok();
        }
    }
}