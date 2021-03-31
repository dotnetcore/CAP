using DotNetCore.CAP.Redis;
using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<CapRedisOptions> configure;
        public RedisOptionsExtension(Action<CapRedisOptions> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            this.configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();
            services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
            services.AddSingleton<IConsumerClientFactory, RedisConsumerClientFactory>();
            services.AddSingleton<ITransport, RedisTransport>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CapRedisOptions>, CapRedisOptionsPostConfigure>());
            services.AddOptions<CapRedisOptions>().Configure(configure);
        }
    }
}
