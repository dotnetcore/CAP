// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using DotNetCore.CAP.Dashboard.GatewayProxy.Requester;
using DotNetCore.CAP.Dashboard.K8s;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard.K8s
{
    internal sealed class K8SDiscoveryOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<K8SDiscoveryOptions>? _options;

        public K8SDiscoveryOptionsExtension(Action<K8SDiscoveryOptions>? option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            var k8SOptions = new K8SDiscoveryOptions();

            _options?.Invoke(k8SOptions);
            services.AddSingleton(k8SOptions);

            services.AddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.AddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            services.AddSingleton<IRequestMapper, RequestMapper>();
            services.AddSingleton<GatewayProxyAgent>();
            services.AddSingleton<INodeDiscoveryProvider, K8SNodeDiscoveryProvider>();
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapDiscoveryOptionsExtensions
    {
        public static CapOptions UseK8sDiscovery(this CapOptions capOptions)
        {
            return capOptions.UseK8sDiscovery(opt => { });
        }

        public static CapOptions UseK8sDiscovery(this CapOptions capOptions, Action<K8SDiscoveryOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            capOptions.RegisterExtension(new K8SDiscoveryOptionsExtension(options));

            return capOptions;
        }
    }
}
