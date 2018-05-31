// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Kafka;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class KafkaCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<KafkaOptions> _configure;

        public KafkaCapOptionsExtension(Action<KafkaOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            var kafkaOptions = new KafkaOptions();
            _configure?.Invoke(kafkaOptions);
            services.AddSingleton(kafkaOptions);

            services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();
            services.AddSingleton<IPublishExecutor, KafkaPublishMessageSender>();
            services.AddSingleton<IPublishMessageSender, KafkaPublishMessageSender>();
            services.AddSingleton<IConnectionPool,ConnectionPool>();
        }
    }
}