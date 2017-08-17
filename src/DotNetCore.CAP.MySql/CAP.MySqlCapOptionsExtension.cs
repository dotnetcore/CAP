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
            services.AddTransient<ICallbackPublisher, CapPublisher>();
            services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            var mysqlOptions = new MySqlOptions();
            _configure(mysqlOptions);

            if (mysqlOptions.DbContextType != null)
            {
                services.AddSingleton(x =>
                {
                    var dbContext = (DbContext)x.GetService(mysqlOptions.DbContextType);
                    mysqlOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
                    return mysqlOptions;
                });
            }
            else
            {
                services.AddSingleton(mysqlOptions);
            }
        }
    }
}