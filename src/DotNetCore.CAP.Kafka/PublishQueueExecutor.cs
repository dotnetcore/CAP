using System;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    public class PublishQueueExecutor : BasePublishQueueExecutor
    {
        private readonly ILogger _logger;
        private readonly KafkaOptions _kafkaOptions;

        public PublishQueueExecutor(IStateChanger stateChanger,
            IOptions<KafkaOptions> options,
            ILogger<PublishQueueExecutor> logger)
            : base(stateChanger, logger)
        {
            _logger = logger;
            _kafkaOptions = options.Value;
        }

        public override Task<OperateResult> PublishAsync(string keyName, string content)
        {
            try
            {
                var config = _kafkaOptions.AsRdkafkaConfig();
                using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8)))
                {
                    producer.ProduceAsync(keyName, null, content);
                    producer.Flush();
                }

                _logger.LogDebug($"kafka topic message [{keyName}] has been published.");

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"kafka topic message [{keyName}] has benn raised an exception of sending. the exception is: {ex.Message}");

                return Task.FromResult(OperateResult.Failed(ex,
                    new OperateError()
                    {
                        Code = ex.HResult.ToString(),
                        Description = ex.Message
                    }));
            }
        }
    }
}