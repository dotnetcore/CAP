using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitMQProducerClient : ICapProducerService
    {
        private readonly ConsistencyOptions _options;
        private readonly ILogger _logger;

        public RabbitMQProducerClient(IOptions<ConsistencyOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger(nameof(RabbitMQProducerClient));
        }

        public Task SendAsync(string topic, string content)
        {
            var factory = new ConnectionFactory() { HostName = _options.BrokerUrlList };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "topic_logs",
                                        type: "topic");

                var body = Encoding.UTF8.GetBytes(content);
                channel.BasicPublish(exchange: "topic_logs",
                                     routingKey: topic,
                                     basicProperties: null,
                                     body: body);

                return Task.CompletedTask;
            }
        }
    }
}