using DotNetCore.CAP.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace DotNetCore.CAP
{
    public class DMOptions: EFOptions
    {
        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; } = default!;
    }
    internal class ConfigureDMOptions : IConfigureOptions<DMOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConfigureDMOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(DMOptions options)
        {
            if (options.DbContextType == null) return;

            if (Helper.IsUsingType<ICapPublisher>(options.DbContextType))
                throw new InvalidOperationException(
                    "We detected that you are using ICapPublisher in DbContext, please change the configuration to use the storage extension directly to avoid circular references! eg:  x.UseDM()");

            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            using var dbContext = (DbContext)provider.GetRequiredService(options.DbContextType);
            var connectionString = dbContext.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(connectionString);
            options.ConnectionString = connectionString;
        }
    }
}
