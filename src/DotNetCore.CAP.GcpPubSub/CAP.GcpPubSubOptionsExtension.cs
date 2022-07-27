// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.GooglePubSub;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class GcpPubSubOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<GcpPubSubOptions> _configure;

        public GcpPubSubOptionsExtension(Action<GcpPubSubOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            services.Configure(_configure);

            services.AddSingleton<IConsumerClientFactory, GcpPubSubConsumerClientFactory>();
            services.AddSingleton<ITransport, GcpPubSubTransport>();
        }
    }
}