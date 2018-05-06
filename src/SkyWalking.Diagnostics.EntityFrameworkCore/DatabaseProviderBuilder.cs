using Microsoft.Extensions.DependencyInjection;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public class DatabaseProviderBuilder
    {
        public IServiceCollection Services { get; }

        public DatabaseProviderBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}