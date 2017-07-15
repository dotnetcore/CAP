using System;
using DotNetCore.CAP.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP
{
    public class KafkaCapOptionsExtension : ICapOptionsExtension
    {
        private Action<KafkaOptions> _configure;

        public KafkaCapOptionsExtension(Action<KafkaOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);

            var kafkaOptions = new KafkaOptions();
            _configure(kafkaOptions);
            services.AddSingleton(kafkaOptions);

            services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();
            services.AddTransient<IQueueExecutor, PublishQueueExecutor>();
        }
    }
}
