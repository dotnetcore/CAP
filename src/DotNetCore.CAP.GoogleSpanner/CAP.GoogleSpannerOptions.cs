using Google.Api.Gax;
using Google.Cloud.Spanner.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Google Spanner Options 
    /// </summary>
    /// <remarks>
    /// At the moment this implementation only supports Google Application Default Credentials. 
    /// </remarks>
    public class GoogleSpannerOptions : EFOptions
    {
        public EmulatorDetection Emulator { get; set; } = EmulatorDetection.None;

        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
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

                var connectionString = dbContext.Database.GetDbConnection().ConnectionString;

                if(string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException(connectionString);
                }

                options.ConnectionString = connectionString;
            }

            SpannerConnectionStringBuilder builder = new(options.ConnectionString)
            {
                EmulatorDetection = options.Emulator,
            };

            options.ConnectionString = builder.ConnectionString;
        }
    }
}
