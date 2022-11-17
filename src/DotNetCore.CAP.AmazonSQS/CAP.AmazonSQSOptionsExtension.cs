// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.AmazonSQS;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class AmazonSQSOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<AmazonSQSOptions> _configure;

        public AmazonSQSOptionsExtension(Action<AmazonSQSOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton(new CapMessageQueueMakerService("Amazon SQS"));
             
            services.Configure(_configure);
            services.AddSingleton<ITransport, AmazonSQSTransport>();
            services.AddSingleton<IConsumerClientFactory, AmazonSQSConsumerClientFactory>();
        }
    }
}