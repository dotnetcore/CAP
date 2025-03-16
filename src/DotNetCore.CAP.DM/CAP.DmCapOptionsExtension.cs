using DotNetCore.CAP.DM;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace DotNetCore.CAP
{
    internal class DMCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<DMOptions> _configure;

        public DMCapOptionsExtension(Action<DMOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton(new CapStorageMarkerService("DM"));
            services.AddSingleton<IDataStorage, DMDataStorage>();
            services.AddSingleton<IStorageInitializer, DMStorageInitializer>();
            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<DMOptions>, ConfigureDMOptions>();
        }
    }
}
