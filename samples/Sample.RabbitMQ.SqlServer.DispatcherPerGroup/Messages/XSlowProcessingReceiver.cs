using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.Messages
{
    [QueueHandlerTopic("slowtopic")]
    public class XSlowProcessingReceiver : QueueHandler
    {
        private readonly ILogger<XSlowProcessingReceiver> _logger;

        public XSlowProcessingReceiver(ILogger<XSlowProcessingReceiver> logger)
        {
            _logger = logger;
        }

        public async Task Handle(TestMessage value)
        {
            _logger.LogInformation($"Starting SLOW processing handler {DateTime.Now:O}: {value.Text}");
            await Task.Delay(10000);
            _logger.LogInformation($"Ending   SLOW processing handler {DateTime.Now:O}: {value.Text}");
        }
    }
}