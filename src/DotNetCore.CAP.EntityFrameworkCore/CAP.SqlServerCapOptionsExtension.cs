using System;
using DotNetCore.CAP.EntityFrameworkCore;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP
{
    public class SqlServerCapOptionsExtension : ICapOptionsExtension
    {
        private Action<SqlServerOptions> _configure;

        public SqlServerCapOptionsExtension(Action<SqlServerOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<IStorage, SqlServerStorage>();
            services.AddScoped<IStorageConnection, SqlServerStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
            services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            services.Configure(_configure);

            var sqlServerOptions = new SqlServerOptions();
            _configure(sqlServerOptions);
            services.AddSingleton(sqlServerOptions);
        }
    }
}
