using Cap.Consistency.EntityFrameworkCore;
using Cap.Consistency.Store;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="ConsistencyBuilder"/> for adding entity framework stores.
    /// </summary>
    public static class ConsistencyEntityFrameworkBuilderExtensions
    {
        /// <summary>
        /// Adds an Entity Framework implementation of message stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <param name="services">The <see cref="ConsistencyBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="ConsistencyBuilder"/> instance this method extends.</returns>
        public static ConsistencyBuilder AddEntityFrameworkStores<TContext>(this ConsistencyBuilder builder)
            where TContext : DbContext {

            builder.Services.AddScoped<IConsistencyMessageStore, ConsistencyMessageStore<TContext>>();

            return builder;
        }
    }
}