using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Producer;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cap.Consistency.Kafka
{
    public class KafkaProducerClient : IProducerClient
    {

        private readonly ConsistencyOptions _options;
        private readonly ILogger _logger;

        public KafkaProducerClient(IOptions<ConsistencyOptions> options, ILoggerFactory loggerFactory) {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger(nameof(KafkaProducerClient));
        }


        public Task SendAsync(string topic, string content) {
            var config = new Dictionary<string, object> { { "bootstrap.servers", _options.BrokerUrlList } };
            try {
                using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8))) {
                    var task = producer.ProduceAsync(topic, null, content);
                    task.ContinueWith(g => {
                        _logger.LogInformation($"publish message to topic:{topic}\r\n -------content:{content}\r\n ");
                    });
                    //producer.Flush(1000);
                    return Task.CompletedTask;
                }
            }
            catch (Exception e) {
                _logger.LogError(new EventId(1), e, $"publish message to [topic:{topic}] ,content:{content}");
                return Task.CompletedTask;
            }
        }
    }
}
