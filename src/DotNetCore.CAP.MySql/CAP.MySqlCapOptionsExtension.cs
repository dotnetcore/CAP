using System;
using DotNetCore.CAP.MySql;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class MySqlCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<MySqlOptions> _configure;

        public MySqlCapOptionsExtension(Action<MySqlOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<IStorage, MySqlStorage>();
            services.AddScoped<IStorageConnection, MySqlStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
            services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            var mysqlOptions = new MySqlOptions();
            _configure(mysqlOptions);

            if (mysqlOptions.DbContextType != null)
            {
                var provider = TempBuildService(services);
                var dbContextObj = provider.GetService(mysqlOptions.DbContextType);
                var dbContext = (DbContext)dbContextObj;
                mysqlOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
            }
            services.AddSingleton(mysqlOptions);
        }

#if NETSTANDARD1_6
        private IServiceProvider TempBuildService(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
#else
        private ServiceProvider TempBuildService(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
#endif
    }
}