// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.AzureServiceBus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class AzureServiceBusOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<AzureServiceBusOptions> _configure;

        public AzureServiceBusOptionsExtension(Action<AzureServiceBusOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            services.Configure(_configure);

            services.AddSingleton<IConsumerClientFactory, AzureServiceBusConsumerClientFactory>();
            services.AddSingleton<IPublishExecutor, AzureServiceBusPublishMessageSender>();
            services.AddSingleton<IPublishMessageSender, AzureServiceBusPublishMessageSender>();
        }
    }
}