using Cap.Consistency;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection"/> for configuring kafka consistence services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds and configures the consistence services for the consitence.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <returns>An <see cref="IServiceCollection"/> for application services.</returns>
        public static ConsistencyBuilder AddKafkaConsistence<TMessage>(this IServiceCollection services)
            where TMessage : class {

            services.TryAddSingleton<ConsistencyMarkerService>();

            return new ConsistencyBuilder(typeof(TMessage), services);
        }
    }
}