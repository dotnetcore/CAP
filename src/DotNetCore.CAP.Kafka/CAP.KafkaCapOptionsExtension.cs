// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            services.TryAddSingleton(kafkaOptions);

            services.TryAddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();
            services.TryAddSingleton<IPublishExecutor, KafkaPublishMessageSender>();
            services.TryAddSingleton<IPublishMessageSender, KafkaPublishMessageSender>();
            services.TryAddSingleton<IConnectionPool,ConnectionPool>();
        }
    }
}