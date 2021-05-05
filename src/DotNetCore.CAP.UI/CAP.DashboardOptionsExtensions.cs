// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using BootstrapBlazor.Components;
using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.GatewayProxy.Requester;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    internal sealed class DashboardOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<DashboardOptions> _options;

        public DashboardOptionsExtension(Action<DashboardOptions> option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            var dashboardOptions = new DashboardOptions();
            _options?.Invoke(dashboardOptions);
            services.AddSingleton(dashboardOptions);
            services.AddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.AddSingleton<IHttpClientCache, MemoryHttpClientCache>();

            services.AddBootstrapBlazor(setupAction: options =>
            {
                options.AdditionalJsonAssemblies = new[] { GetType().Assembly };
            });

            services.AddRequestLocalization<IOptions<BootstrapBlazorOptions>>((localizerOption, blazorOption) =>
            {
                var supportedCultures = blazorOption.Value.GetSupportedCultures();

                localizerOption.SupportedCultures = supportedCultures;
                localizerOption.SupportedUICultures = supportedCultures;
            });
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseDashboard(this CapOptions capOptions)
        {
            return capOptions.UseDashboard(opt => { });
        }

        public static CapOptions UseDashboard(this CapOptions capOptions, Action<DashboardOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            capOptions.RegisterExtension(new DashboardOptionsExtension(options));

            return capOptions;
        }
    }
}