using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace DotNetCore.CAP.GoogleSpanner
{
    public class GoogleSpannerCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<GoogleSpannerOptions> _configure;

        public GoogleSpannerCapOptionsExtension(Action<GoogleSpannerOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();
            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<GoogleSpannerOptions>, ConfigureGoogleSpannerOptions>();

            services.AddSingleton<IDataStorage, GoogleSpannerDataStorage>();
            services.AddSingleton<IStorageInitializer, GoogleSpannerStorageInitializer>();
            services.AddTransient<ICapTransaction, GoogleSpannerCapTransaction>();
        }
    }
}
