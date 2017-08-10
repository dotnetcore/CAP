using System;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class PostgreSqlCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<PostgreSqlOptions> _configure;

        public PostgreSqlCapOptionsExtension(Action<PostgreSqlOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<IStorage, PostgreSqlStorage>();
            services.AddScoped<IStorageConnection, PostgreSqlStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
            services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            var postgreSqlOptions = new PostgreSqlOptions();
            _configure(postgreSqlOptions);

            if (postgreSqlOptions.DbContextType != null)
            {
                var provider = TempBuildService(services);
                var dbContextObj = provider.GetService(postgreSqlOptions.DbContextType);
                var dbContext = (DbContext)dbContextObj;
                postgreSqlOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
            }
            services.AddSingleton(postgreSqlOptions);
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