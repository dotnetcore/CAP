using DotNetCore.CAP;
using System;
using System.Web.Mvc;

namespace Sample.NetFramework.Postgres.Controllers
{
    public class HomeController : Controller
    {
        private ICapPublisher _capPublisher;

        public HomeController(ICapPublisher capPublisher)
        {
            _capPublisher = capPublisher;
        }

        // GET: Home
        public ActionResult Publish()
        {
            _capPublisher.PublishAsync("SampleTopic", DateTime.Now);
            Console.WriteLine("Published to SampleTopic");
            return null;
        }

        [CapSubscribe("SampleTopic")]
        public void Subscribe(Object obj)
        {
            Console.WriteLine($"Received {obj} from SampleTopic queue");
        }
    }
}