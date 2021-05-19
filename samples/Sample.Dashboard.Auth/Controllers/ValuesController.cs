using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sample.Dashboard.Blazor.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICapPublisher _capBus;
        private readonly ILogger<ValuesController> _logger;
        private const string CapGroup = "sample.rabbitmq.postgres.dashboard";

        public ValuesController(ICapPublisher capPublisher, ILogger<ValuesController> logger)
        {
            _capBus = capPublisher;
            _logger = logger;
        }

        [Route("publish")]
        public async Task<IActionResult> Publish()
        {
            await _capBus.PublishAsync(CapGroup, new Person()
            {
                Id = 123,
                Name = "Bar"
            });

            return Ok();
        }

        [NonAction]
        [CapSubscribe(CapGroup)]
        public void Subscribe(Person p, [FromCap] CapHeader header)
        {
            var id = header[Headers.MessageId];

            _logger.LogInformation($@"{DateTime.Now} Subscriber invoked for message {id}, Info: {p}");
        }

        [NonAction]
        [CapSubscribe(CapGroup,Group = "foo")]
        public void Subscribe2(Person p, [FromCap] CapHeader header)
        {
            var id = header[Headers.MessageId];

            _logger.LogInformation($@"{DateTime.Now} Subscriber invoked for message {id}, Info: {p}");
        }

        [NonAction]
        [CapSubscribe(CapGroup, Group = "bar")]
        public void Subscribe3(Person p, [FromCap] CapHeader header)
        {
            var id = header[Headers.MessageId];

            _logger.LogInformation($@"{DateTime.Now} Subscriber invoked for message {id}, Info: {p}");
        }

        [NonAction]
        [CapSubscribe("MSubscribe.AAA")]
        public void MSubscribe(Person p, [FromCap] CapHeader header)
        {
            var id = header[Headers.MessageId];

            _logger.LogInformation($@"{DateTime.Now} Subscriber invoked for message {id}, Info: {p}");
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}