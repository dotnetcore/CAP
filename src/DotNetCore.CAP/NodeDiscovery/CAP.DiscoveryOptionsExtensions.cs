using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP
{
    using DotNetCore.CAP.NodeDiscovery;
    using Microsoft.Extensions.DependencyInjection;

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

            services.AddSingleton<IDiscoveryProviderFactory, DiscoveryProviderFactory>();
            services.AddSingleton<IProcessingServer, ConsulProcessingNodeServer>();
            services.AddSingleton<INodeDiscoveryProvider>(x =>
            {
                var configOptions = x.GetService<DiscoveryOptions>();
                var factory = x.GetService<IDiscoveryProviderFactory>();
                return factory.Create(configOptions);
            });
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using DotNetCore.CAP;

    public static class CapDiscoveryOptionsExtensions
    {
        public static CapOptions UseDiscovery(this CapOptions capOptions)
        {
            return capOptions.UseDiscovery(opt => { });
        }

        public static CapOptions UseDiscovery(this CapOptions capOptions, Action<DiscoveryOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            capOptions.RegisterExtension(new DiscoveryOptionsExtension(options));

            return capOptions;
        }
    }
}