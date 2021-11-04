using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.Messages
{
    [QueueHandlerTopic("fasttopic")]
    public class FastProcessingReceiver : QueueHandler
    {
        private readonly ILogger<FastProcessingReceiver> _logger;

        public FastProcessingReceiver(ILogger<FastProcessingReceiver> logger)
        {
            _logger = logger;
        }

        public async Task Handle(TestMessage value)
        {
            _logger.LogInformation($"Starting FAST processing handler {DateTime.Now:O}: {value.Text}");
            await Task.Delay(200);
            _logger.LogInformation($"Ending   FAST processing handler {DateTime.Now:O}: {value.Text}");
        }
    }
}