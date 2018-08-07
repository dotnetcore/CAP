// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class RabbitMQCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<RabbitMQOptions> _configure;

        public RabbitMQCapOptionsExtension(Action<RabbitMQOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            var options = new RabbitMQOptions();
            _configure?.Invoke(options);
            services.TryAddSingleton(options);

            services.TryAddSingleton<IConsumerClientFactory, RabbitMQConsumerClientFactory>();
            services.TryAddSingleton<IConnectionChannelPool, ConnectionChannelPool>();
            services.TryAddSingleton<IPublishExecutor, RabbitMQPublishMessageSender>();
            services.TryAddSingleton<IPublishMessageSender, RabbitMQPublishMessageSender>();
        }
    }
}