// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using DotNetCore.CAP.RedisStreams;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class RedisOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<CapRedisOptions> _configure;

        public RedisOptionsExtension(Action<CapRedisOptions> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();
            services.AddSingleton<IRedisStreamManager, RedisStreamManager>();
            services.AddSingleton<IConsumerClientFactory, RedisConsumerClientFactory>();
            services.AddSingleton<ITransport, RedisTransport>();
            services.AddSingleton<IRedisConnectionPool, RedisConnectionPool>();
            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IPostConfigureOptions<CapRedisOptions>, CapRedisOptionsPostConfigure>());
            services.AddOptions<CapRedisOptions>().Configure(_configure);
        }
    }

    internal class CapRedisOptionsPostConfigure : IPostConfigureOptions<CapRedisOptions>
    {
        public void PostConfigure(string name, CapRedisOptions options)
        {
            options.Configuration ??= new ConfigurationOptions();

            if (options.StreamEntriesCount == default)
                options.StreamEntriesCount = 10;

            if (options.ConnectionPoolSize == default)
                options.ConnectionPoolSize = 10;

            if (!options.Configuration.EndPoints.Any())
            {
                options.Configuration.EndPoints.Add(IPAddress.Loopback, 0);
                options.Configuration.SetDefaultPorts();
            }
        }
    }
}