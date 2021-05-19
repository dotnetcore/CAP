// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    internal sealed class DiscoveryOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<DiscoveryOptions> _options;

        public DiscoveryOptionsExtension(Action<DiscoveryOptions> option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            var discoveryOptions = new DiscoveryOptions();

            _options?.Invoke(discoveryOptions);
            services.AddSingleton(discoveryOptions);

            services.AddSingleton<IProcessingServer, ConsulProcessingNodeServer>();
            services.AddSingleton<INodeDiscoveryProvider, ConsulNodeDiscoveryProvider>();
        }
    }

    public static class CapDiscoveryOptionsExtensions
    {
        public static CapOptions UseDiscovery(this CapOptions capOptions)
        {
            return capOptions.UseDiscovery(opt => { });
        }

        public static CapOptions UseDiscovery(this CapOptions capOptions, Action<DiscoveryOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            capOptions.RegisterExtension(new DiscoveryOptionsExtension(options));

            return capOptions;
        }
    }
}