// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.ZeroMQ;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class ZeroMQCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<ZeroMQOptions> _configure;

        public ZeroMQCapOptionsExtension(Action<ZeroMQOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();
             
            services.Configure(_configure);
            services.AddSingleton<ITransport, ZeroMQTransport>();
            services.AddSingleton<IConsumerClientFactory, ZeroMQConsumerClientFactory>();
            services.AddSingleton<IConnectionChannelPool, ConnectionChannelPool>();
        }
    }
}