using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Redis.SqlServer.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICapPublisher publisher;

        public HomeController(ILogger<HomeController> logger, ICapPublisher publisher)
        {
            _logger = logger;
            this.publisher = publisher;
        }

        [HttpGet]
        public async Task Publish()
        {
            await publisher.PublishAsync("test-message", DateTime.UtcNow);
        }

        [CapSubscribe("test-message")]
        [NonAction]
        public void Subscribe(DateTime date, [FromCap] IDictionary<string, string> headers)
        {
            var str = string.Join(",", headers.Select(kv => $"({kv.Key}:{kv.Value})"));
            _logger.LogInformation($"test-message subscribed with value {date}, headers : {str}");
        }
    }
}
