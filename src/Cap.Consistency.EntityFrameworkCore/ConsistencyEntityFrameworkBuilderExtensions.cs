using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Cap.Consistency;
using Cap.Consistency.EntityFrameworkCore;

namespace Cap.Consistency.EntityFrameworkCore
{
    /// <summary>
    /// Contains extension methods to <see cref="ConsistencyBuilder"/> for adding entity framework stores.
    /// </summary>
    public static class ConsistencyEntityFrameworkBuilderExtensions
    {
        /// <summary>
        /// Adds an Entity Framework implementation of identity information stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <param name="builder">The <see cref="ConsistencyBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="ConsistencyBuilder"/> instance this method extends.</returns>
        public static ConsistencyBuilder AddEntityFrameworkStores<TContext>(this ConsistencyBuilder builder)
            where TContext : DbContext {
            builder.Services.TryAdd(GetDefaultServices(builder.MessageType, typeof(TContext)));
            return builder;
        }

        /// <summary>
        /// Adds an Entity Framework implementation of identity information stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <typeparam name="TKey">The type of the primary key used for the users and roles.</typeparam>
        /// <param name="builder">The <see cref="ConsistencyBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="ConsistencyBuilder"/> instance this method extends.</returns>
        public static ConsistencyBuilder AddEntityFrameworkStores<TContext, TKey>(this ConsistencyBuilder builder)
            where TContext : DbContext
            where TKey : IEquatable<TKey> {
            builder.Services.TryAdd(GetDefaultServices(builder.MessageType, typeof(TContext), typeof(TKey)));
            return builder;
        }

        private static IServiceCollection GetDefaultServices(Type messageType, Type contextType, Type keyType = null) {
            Type messageStoreType;
            keyType = keyType ?? typeof(string);
            messageStoreType = typeof(IConsistencyMessageStore<>).MakeGenericType(messageType, contextType, keyType);

            var services = new ServiceCollection();
            services.AddScoped(
                typeof(IConsistencyMessageStore<>).MakeGenericType(messageStoreType),
                messageStoreType);
            return services;
        }
    }
}
