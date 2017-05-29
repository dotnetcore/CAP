using System;
using System.Reflection;
using Cap.Consistency;
using Cap.Consistency.EntityFrameworkCore;
using Cap.Consistency.Infrastructure;
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
        /// <param name="builder">The <see cref="ConsistencyBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="ConsistencyBuilder"/> instance this method extends.</returns>
        public static ConsistencyBuilder AddEntityFrameworkStores<TContext>(this ConsistencyBuilder builder)
            where TContext : DbContext {

            builder.Services.AddScoped(typeof(IConsistencyMessageStore<>).MakeGenericType(builder.MessageType),
                typeof(ConsistencyMessageStore<,>).MakeGenericType(typeof(ConsistencyMessage), typeof(TContext)));

            return builder;
        }

        private static TypeInfo FindGenericBaseType(Type currentType, Type genericBaseType) {
            var type = currentType.GetTypeInfo();
            while (type.BaseType != null) {
                type = type.BaseType.GetTypeInfo();
                var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (genericType != null && genericType == genericBaseType) {
                    return type;
                }
            }
            return null;
        }
    }
}