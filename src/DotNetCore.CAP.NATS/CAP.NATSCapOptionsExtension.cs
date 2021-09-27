// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.NATS;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class NATSCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<NATSOptions> _configure;

        public NATSCapOptionsExtension(Action<NATSOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            services.Configure(_configure);

            services.AddSingleton<ITransport, NATSTransport>();
            services.AddSingleton<IConsumerClientFactory, NATSConsumerClientFactory>();
            services.AddSingleton<IConnectionPool, ConnectionPool>();
        }
    }
}