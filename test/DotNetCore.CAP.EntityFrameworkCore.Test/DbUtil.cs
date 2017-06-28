using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public static class DbUtil
    {
        public static IServiceCollection ConfigureDbServices(string connectionString, IServiceCollection services = null) {
            return ConfigureDbServices<CapDbContext>(connectionString, services);
        }

        public static IServiceCollection ConfigureDbServices<TContext>(string connectionString, IServiceCollection services = null) where TContext : DbContext {
            if (services == null) {
                services = new ServiceCollection();
            }
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDbContext<TContext>(options => options.UseSqlServer(connectionString));
            return services;
        }

        public static TContext Create<TContext>(string connectionString) where TContext : DbContext {
            var serviceProvider = ConfigureDbServices<TContext>(connectionString).BuildServiceProvider();
            return serviceProvider.GetRequiredService<TContext>();
        }
    }
}