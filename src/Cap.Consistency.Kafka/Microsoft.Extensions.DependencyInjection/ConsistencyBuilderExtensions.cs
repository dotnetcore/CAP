using Cap.Consistency.Consumer;
using Cap.Consistency.Job;
using Cap.Consistency.Kafka;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConsistencyBuilderExtensions
    {
        public static ConsistencyBuilder AddKafka(this ConsistencyBuilder builder) {

            builder.Services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();

            builder.Services.AddTransient<IJobProcessor, KafkaJobProcessor>();

            return builder;
        }
    }
}