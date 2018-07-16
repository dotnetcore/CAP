using System;
using DotNetCore.CAP.GBase8t;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP
{
    internal class GBase8tCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<GBase8tOptions> _configure;

        public GBase8tCapOptionsExtension(Action<GBase8tOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapDatabaseStorageMarkerService>();
            services.AddSingleton<IStorage, GBase8tStorage>();
            services.AddSingleton<IStorageConnection, GBase8tStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
            services.AddScoped<ICallbackPublisher, CapPublisher>();
            services.AddTransient<ICollectProcessor, GBase8tCollectProcessor>();

            AddGBase8tOptions(services);
        }

        private void AddGBase8tOptions(IServiceCollection services)
        {
            var gBase8tOptions = new GBase8tOptions();

            _configure(gBase8tOptions);

            if (gBase8tOptions.DbContextType != null)
            {
                services.AddSingleton(x =>
                {
                    using (var scope = x.CreateScope())
                    {
                        var provider = scope.ServiceProvider;
                        var dbContext = (DbContext)provider.GetService(gBase8tOptions.DbContextType);
                        gBase8tOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
                        return gBase8tOptions;
                    }
                });
            }
            else
            {
                services.AddSingleton(gBase8tOptions);
            }
        }
    }
}
