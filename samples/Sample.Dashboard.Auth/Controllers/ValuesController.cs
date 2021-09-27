using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sample.Dashboard.Auth.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICapPublisher _capBus;
        private readonly ILogger<ValuesController> _logger;
        private const string MyTopic = "sample.dashboard.auth";

        public ValuesController(ICapPublisher capPublisher, ILogger<ValuesController> logger)
        {
            _capBus = capPublisher;
            _logger = logger;
        }

        [Route("publish")]
        public async Task<IActionResult> Publish()
        {
            await _capBus.PublishAsync(MyTopic, new Person()
            {
                Id = new Random().Next(1, 100),
                Name = "Bar"
            });

            return Ok();
        }

        [NonAction]
        [CapSubscribe(MyTopic)]
        public void Subscribe(Person p, [FromCap] CapHeader header)
        {
            _logger.LogInformation("Subscribe Invoked: " + MyTopic + p);
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}