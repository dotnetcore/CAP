// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using DotNetCore.CAP.Dashboard.GatewayProxy.Requester;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    internal sealed class ConsulDiscoveryOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<ConsulDiscoveryOptions> _options;

        public ConsulDiscoveryOptionsExtension(Action<ConsulDiscoveryOptions> option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            var discoveryOptions = new ConsulDiscoveryOptions();

            _options?.Invoke(discoveryOptions);
            services.AddSingleton(discoveryOptions);

            services.AddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.AddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            services.AddSingleton<IRequestMapper, RequestMapper>();
            services.AddSingleton<GatewayProxyAgent>();
            services.AddSingleton<IProcessingServer, ConsulProcessingNodeServer>();
            services.AddSingleton<INodeDiscoveryProvider, ConsulNodeDiscoveryProvider>();
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapDiscoveryOptionsExtensions
    {
        /// <summary>
        /// Default use kubernetes as service discovery.
        /// </summary>
        public static CapOptions UseConsulDiscovery(this CapOptions capOptions)
        {
            return capOptions.UseConsulDiscovery(_ => { });
        }

        public static CapOptions UseConsulDiscovery(this CapOptions capOptions, Action<ConsulDiscoveryOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            capOptions.RegisterExtension(new ConsulDiscoveryOptionsExtension(options));

            return capOptions;
        }
    }
}