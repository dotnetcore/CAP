using DotNetCore.CAP;
using DotNetCore.CAP.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="CapBuilder"/> for adding entity framework stores.
    /// </summary>
    public static class ConsistencyEntityFrameworkBuilderExtensions
    {
        /// <summary>
        /// Adds an Entity Framework implementation of message stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <param name="services">The <see cref="CapBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="CapBuilder"/> instance this method extends.</returns>
        public static CapBuilder AddEntityFrameworkStores<TContext>(this CapBuilder builder)
            where TContext : DbContext
        {
            builder.Services.AddScoped<ICapMessageStore, CapMessageStore<TContext>>();

            return builder;
        }
    }
}