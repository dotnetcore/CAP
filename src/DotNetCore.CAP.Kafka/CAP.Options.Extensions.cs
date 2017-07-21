using System;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseKafka(this CapOptions options, string bootstrapServers)
        {
            return options.UseKafka(opt =>
            {
                opt.Servers = bootstrapServers;
            });
        }

        public static CapOptions UseKafka(this CapOptions options, Action<KafkaOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new KafkaCapOptionsExtension(configure));

            return options;
        }
    }
}