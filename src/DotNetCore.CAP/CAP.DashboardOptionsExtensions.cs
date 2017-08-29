using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

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
            var dashboardOptions = new DashboardOptions();
            _options?.Invoke(dashboardOptions);
            services.AddSingleton(dashboardOptions);
        }
    }


    public static class CapOptionsExtensions
    {
        /// <summary>
        /// Configuration to use kafka in CAP.
        /// </summary>
        /// <param name="options">Provides programmatic configuration for the kafka .</param>
        /// <returns></returns>
        public static CapOptions UseDashboard(this CapOptions capOptions, Action<DashboardOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            capOptions.RegisterExtension(new DashboardOptionsExtension(options));

            return capOptions;
        }
    }

}
