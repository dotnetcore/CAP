using Cap.Consistency.Consumer;
using Cap.Consistency.Kafka;
using Cap.Consistency.Producer;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConsistencyBuilderExtensions
    {
        public static ConsistencyBuilder AddKafka(this ConsistencyBuilder builder) {
            builder.Services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();

            builder.Services.AddTransient<IProducerClient, KafkaProducerClient>();

            return builder;
        }
    }
}