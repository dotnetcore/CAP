using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.Consumer.Kafka
{

    public interface IKafkaTaskSchedule : ITaskSchedule { }


    public class KafkaTaskSchedule : IKafkaTaskSchedule
    {

        private readonly ILogger _logger;


        public KafkaTaskSchedule(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<KafkaTaskSchedule>();

        }
        public void Start(IReadOnlyList<ConsumerExecutorDescriptor> methods) {
            throw new NotImplementedException();
        }

        public void Stop() {
            throw new NotImplementedException();
        }
    }
}
