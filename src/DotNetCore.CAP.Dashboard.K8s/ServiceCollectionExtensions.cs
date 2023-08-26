// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.K8s;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection" /> for configuring consistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Run only CAP dashboard to view data based on the nodes discovered in Kubernetes.
    /// </summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="option">An action to configure the <see cref="DashboardOptions" />.</param>
    /// <param name="k8SOption">An action to configure the <see cref="K8sDiscoveryOptions" />.</param>
    /// <returns>An <see cref="CapBuilder" /> for application services.</returns>
    public static IServiceCollection AddCapDashboardStandalone(this IServiceCollection services,
        Action<DashboardOptions>? option = null,
        Action<K8sDiscoveryOptions>? k8SOption = null)
    {
        new DashboardOptionsExtension(option ?? (_ => { })).AddServices(services);
        new K8sDiscoveryOptionsExtension(k8SOption).AddServices(services);
        return services;
    }
}