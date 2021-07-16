using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Samples.Redis.SqlServer.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICapPublisher _publisher;

        public HomeController(ILogger<HomeController> logger, ICapPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        [HttpGet]
        public async Task Publish()
        {
            await _publisher.PublishAsync("test-message", new Person() { Age = 11, Name = "James" });
        }

        [CapSubscribe("test-message")]
        [NonAction]
        public void Subscribe(Person p)
        {
            _logger.LogInformation($"test-message subscribed with value --> " + p);
        }
    }

    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public override string ToString()
        {
            return "Name:" + Name + ", Age:" + Age;
        }
    }
}
