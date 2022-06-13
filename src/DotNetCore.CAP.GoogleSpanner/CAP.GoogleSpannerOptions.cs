using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GoogleSpanner
{
    public class GoogleSpannerOptions : EFOptions
    {
        /// <summary>
        /// The GCP <c>Project</c> ID.
        /// </summary>
        public string ProjectId { get; set; }

        public string InstanceId { get; set; }

        public string DatabaseId { get; set; }

        public bool IsEmulator { get; set; } = false;

        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; }
    }

    internal class ConfigureGoogleSpannerOptions : IConfigureOptions<GoogleSpannerOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConfigureGoogleSpannerOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(GoogleSpannerOptions options)
        {
            if (options.DbContextType != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider;
                using var dbContext = (DbContext)provider.GetRequiredService(options.DbContextType);
                options.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
            }
        }
    }
}
