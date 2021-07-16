using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Samples.Redis.SqlServer.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICapPublisher _publisher;
        private readonly IOptions<CapOptions> _options;

        public HomeController(ILogger<HomeController> logger, ICapPublisher publisher, IOptions<CapOptions> options)
        {
            _logger = logger;
            _publisher = publisher;
            this._options = options;
        }

        [HttpGet]
        public async Task Publish([FromQuery] string message = "test-message")
        {
            await _publisher.PublishAsync(message, new Person() { Age = 11, Name = "James" });
        }

        [CapSubscribe("test-message")]
        [CapSubscribe("test-message-1")]
        [CapSubscribe("test-message-2")]
        [CapSubscribe("test-message-3")]
        [NonAction]
        public void Subscribe(Person p, [FromCap] CapHeader header)
        {
            _logger.LogInformation($"{header[Headers.MessageName]} subscribed with value --> " + p);
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
