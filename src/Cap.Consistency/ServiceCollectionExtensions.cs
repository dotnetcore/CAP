using Cap.Consistency;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection"/> for configuring consistence services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds and configures the consistence services for the consitence.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <returns>An <see cref="ConsistencyBuilder"/> for application services.</returns>
        public static ConsistencyBuilder AddConsistency<TMessage>(this IServiceCollection services)
            where TMessage : class {
            return services.AddConsistency<TMessage>(setupAction: null);
        }

        /// <summary>
        /// Adds and configures the consistence services for the consitence.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">An action to configure the <see cref="ConsistencyOptions"/>.</param>
        /// <returns>An <see cref="ConsistencyBuilder"/> for application services.</returns>
        public static ConsistencyBuilder AddConsistency<TMessage>(this IServiceCollection services, Action<ConsistencyOptions> setupAction)
            where TMessage : class {

            services.TryAddSingleton<ConsistencyMarkerService>();

            services.TryAddScoped<ConsistencyMessageManager<TMessage>, ConsistencyMessageManager<TMessage>>();

            if (setupAction != null) {
                services.Configure(setupAction);
            }

            return new ConsistencyBuilder(typeof(TMessage), services);
        }
    }
}