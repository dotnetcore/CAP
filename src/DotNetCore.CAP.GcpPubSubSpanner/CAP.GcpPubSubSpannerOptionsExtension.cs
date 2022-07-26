﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.GooglePubSub;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class GcpPubSubSpannerOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<GcpPubSubSpannerOptions> _configure;

        public GcpPubSubSpannerOptionsExtension(Action<GcpPubSubSpannerOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            services.Configure(_configure);

            services.AddSingleton<IConsumerClientFactory, GcpPubSubSpannerConsumerClientFactory>();
            services.AddSingleton<ITransport, GcpPubSubSpannerTransport>();
        }
    }
}