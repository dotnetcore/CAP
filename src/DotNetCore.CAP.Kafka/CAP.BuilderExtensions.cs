using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Job;
using DotNetCore.CAP.Kafka;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="CapBuilder"/> for adding kafka service.
    /// </summary>
    public static class CapBuilderExtensions
    {
        /// <summary>
        ///  Adds an Kafka implementation of CAP messages queue.
        /// </summary>
        /// <param name="builder">The <see cref="CapBuilder"/> instance this method extends</param>
        /// <param name="setupAction">An action to configure the <see cref="KafkaOptions"/>.</param>
        /// <returns>An <see cref="CapBuilder"/> for creating and configuring the CAP system.</returns>
        public static CapBuilder AddKafka(this CapBuilder builder, Action<KafkaOptions> setupAction)
        {
            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

            builder.Services.Configure(setupAction);

            builder.Services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();

            builder.Services.AddTransient<IJobProcessor, KafkaJobProcessor>();

            return builder;
        }
    }
}