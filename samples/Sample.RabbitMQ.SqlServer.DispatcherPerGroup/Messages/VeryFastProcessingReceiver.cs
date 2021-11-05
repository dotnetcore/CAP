using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.Messages
{
    [QueueHandlerTopic("fasttopic")]
    public class VeryFastProcessingReceiver : QueueHandler
    {
        private readonly ILogger<VeryFastProcessingReceiver> _logger;

        public VeryFastProcessingReceiver(ILogger<VeryFastProcessingReceiver> logger)
        {
            _logger = logger;
        }

        public async Task Handle(TestMessage value)
        {
            _logger.LogInformation($"Starting FAST processing handler {DateTime.Now:O}: {value.Text}");
            await Task.Delay(50);
            _logger.LogInformation($"Ending   FAST processing handler {DateTime.Now:O}: {value.Text}");
        }
    }
}