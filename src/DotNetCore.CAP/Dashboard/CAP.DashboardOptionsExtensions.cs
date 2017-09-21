using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP
{
    using DotNetCore.CAP.Dashboard;
    using DotNetCore.CAP.Dashboard.GatewayProxy;
    using DotNetCore.CAP.Dashboard.GatewayProxy.Requester;
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class DashboardOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<DashboardOptions> _options;

        public DashboardOptionsExtension(Action<DashboardOptions> option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            var dashboardOptions = new DashboardOptions();
            _options?.Invoke(dashboardOptions);
            services.AddSingleton(dashboardOptions); 
            services.AddSingleton(DashboardRoutes.Routes);          
            services.AddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.AddSingleton<IHttpClientCache, MemoryHttpClientCache>();

            services.AddScoped<IRequestScopedDataRepository, HttpDataRepository>();
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using DotNetCore.CAP;

    public static class CapOptionsExtensions
    {
        public static CapOptions UseDashboard(this CapOptions capOptions)
        {
            return capOptions.UseDashboard(opt => {});
        }

        public static CapOptions UseDashboard(this CapOptions capOptions, Action<DashboardOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            capOptions.RegisterExtension(new DashboardOptionsExtension(options));

            return capOptions;
        }
    }
}