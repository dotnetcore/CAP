using System;
using Microsoft.EntityFrameworkCore;
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
            services.AddSingleton<IStorage, EFStorage>();
            services.AddScoped<IStorageConnection, EFStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
            services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            services.Configure(_configure);

            var sqlServerOptions = new SqlServerOptions();
            _configure(sqlServerOptions);
            services.AddSingleton(sqlServerOptions);

            services.AddDbContext<CapDbContext>(options =>
            {
                options.UseSqlServer(sqlServerOptions.ConnectionString, sqlOpts =>
                {
                    sqlOpts.MigrationsHistoryTable(
                        sqlServerOptions.MigrationsHistoryTableName,
                        sqlServerOptions.MigrationsHistoryTableSchema ?? sqlServerOptions.Schema);
                });
            });
        }
    }
}
