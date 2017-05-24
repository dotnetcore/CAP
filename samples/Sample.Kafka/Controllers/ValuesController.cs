using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cap.Consistency.Consumer;
using Cap.Consistency.Kafka;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Kafka.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, IConsumerService
    {

        [KafkaTopic("zzwl.topic.finace.callBack", IsOneWay = true)]
        public void KafkaTest() {
            Console.WriteLine("kafka test invoked");
        }
    }
}
