using System;
using DotNetCore.CAP;
using DotNetCore.CAP.EntityFrameworkCore;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="CapBuilder"/> for adding entity framework stores.
    /// </summary>
    public static class CapEntityFrameworkBuilderExtensions
    {
        /// <summary>
        /// Adds an Entity Framework implementation of message stores.
        /// </summary>
        public static CapBuilder AddEntityFrameworkStores<TContext>(this CapBuilder builder, Action<SqlServerOptions> actionOptions)
            where TContext : DbContext
        {
      
            //builder.Services.AddScoped<ICapMessageStore, CapMessageStore<TContext>>();

            builder.Services.AddSingleton<IStorage, EFStorage>();
            builder.Services.AddScoped<IStorageConnection, EFStorageConnection>();

            builder.Services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            builder.Services.Configure(actionOptions);
            
            var sqlServerOptions = new SqlServerOptions();
            sqlServerOptions.DbContextType = typeof(TContext);
            actionOptions(sqlServerOptions);
            builder.Services.AddSingleton(sqlServerOptions);

            builder.Services.AddDbContext<CapDbContext>(options =>
            {
                options.UseSqlServer(sqlServerOptions.ConnectionString, sqlOpts =>
                {
                    sqlOpts.MigrationsHistoryTable(
                        sqlServerOptions.MigrationsHistoryTableName,
                        sqlServerOptions.MigrationsHistoryTableSchema ?? sqlServerOptions.Schema);
                });
            });

            return builder;
        }

         
    }
}