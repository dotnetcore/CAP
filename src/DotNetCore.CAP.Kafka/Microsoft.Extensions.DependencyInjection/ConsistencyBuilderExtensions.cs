using DotNetCore.CAP;
using DotNetCore.CAP.Job;
using DotNetCore.CAP.Kafka;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConsistencyBuilderExtensions
    {
        public static CapBuilder AddKafka(this CapBuilder builder)
        {
            builder.Services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();

            builder.Services.AddTransient<IJobProcessor, KafkaJobProcessor>();

            return builder;
        }
    }
}