using System;
using DotNetCore.CAP;
using DotNetCore.CAP.EntityFrameworkCore;
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
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <returns>The <see cref="CapBuilder"/> instance this method extends.</returns>
        public static CapBuilder AddEntityFrameworkStores<TContext>(this CapBuilder builder)
            where TContext : DbContext
        {
            builder.Services.AddScoped<ICapMessageStore, CapMessageStore<TContext>>();

            builder.Services.AddScoped<IStorage, EFStorage>();
            builder.Services.AddScoped<IStorageConnection, EFStorageConnection<TContext>>();

            return builder;
        }
         

        public static CapBuilder AddEntityFrameworkStores<TContext>(this CapBuilder builder, Action<EFOptions> options)
            where TContext : DbContext
        {
            builder.Services.AddScoped<ICapMessageStore, CapMessageStore<TContext>>();

            builder.Services.AddScoped<IStorage, EFStorage>();
            builder.Services.AddScoped<IStorageConnection, EFStorageConnection<TContext>>();
            builder.Services.Configure(options);

            return builder;
        }


    }
}