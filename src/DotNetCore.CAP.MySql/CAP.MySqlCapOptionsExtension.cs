using System;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class MySqlCapOptionsExtension : ICapOptionsExtension
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

            var sqlServerOptions = new MySqlOptions();
            _configure(sqlServerOptions);

            if (sqlServerOptions.DbContextType != null)
            {
                var provider = TempBuildService(services);
                var dbContextObj = provider.GetService(sqlServerOptions.DbContextType);
                var dbContext = (DbContext)dbContextObj;
                sqlServerOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
            }
            services.AddSingleton(sqlServerOptions);
        }

        private IServiceProvider TempBuildService(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
    }
}