using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Job;
using DotNetCore.CAP.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapBuilderExtensions
    {
        public static CapBuilder AddRabbitMQ(this CapBuilder builder, Action<RabbitMQOptions> setupOptions)
        {
            if (setupOptions == null) throw new ArgumentNullException(nameof(setupOptions));

            builder.Services.Configure(setupOptions);

            builder.Services.AddSingleton<IConsumerClientFactory, RabbitMQConsumerClientFactory>();

            builder.Services.AddTransient<IMessageJobProcessor, RabbitJobProcessor>();

            return builder;
        }
    }
}