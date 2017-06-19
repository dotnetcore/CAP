using System;
using System.Collections.Generic;
using System.Reflection;
using Cap.Consistency;
using Cap.Consistency.Abstractions.ModelBinding;
using Cap.Consistency.Consumer;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Internal;
using Cap.Consistency.Store;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        public static ConsistencyBuilder AddConsistency(this IServiceCollection services) {
            services.AddConsistency(x => new ConsistencyOptions());

            return new ConsistencyBuilder(services);
        }

        /// <summary>
        /// Adds and configures the consistence services for the consitence.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">An action to configure the <see cref="ConsistencyOptions"/>.</param>
        /// <returns>An <see cref="ConsistencyBuilder"/> for application services.</returns>
        public static ConsistencyBuilder AddConsistency(this IServiceCollection services, Action<ConsistencyOptions> setupAction) {
            services.TryAddSingleton<ConsistencyMarkerService>();

            services.Configure(setupAction);

            var IConsumerListenerServices = new Dictionary<Type, Type>();
            foreach (var rejectedServices in services) {
                if (rejectedServices.ImplementationType != null && typeof(IConsumerService).IsAssignableFrom(rejectedServices.ImplementationType))
                    IConsumerListenerServices.Add(typeof(IConsumerService), rejectedServices.ImplementationType);
            }

            foreach (var service in IConsumerListenerServices) {
                services.AddSingleton(service.Key, service.Value);
            }

            var types = Assembly.GetEntryAssembly().ExportedTypes;
            foreach (var type in types) {
                if (typeof(IConsumerService).IsAssignableFrom(type)) {
                    services.AddSingleton(typeof(IConsumerService), type);
                }
            }

            services.TryAddSingleton<IConsumerExcutorSelector, ConsumerExcutorSelector>();
            services.TryAddSingleton<IModelBinder, DefaultModelBinder>();
            services.TryAddSingleton<IConsumerInvokerFactory, ConsumerInvokerFactory>();
            services.TryAddSingleton<MethodMatcherCache>();

            services.TryAddSingleton(typeof(ITopicServer), typeof(ConsumerHandler));

            return new ConsistencyBuilder(services);
        }


        public static ConsistencyBuilder AddMessageStore<T>(this ConsistencyBuilder build)
            where T : class, IConsistencyMessageStore {
            build.Services.AddScoped<IConsistencyMessageStore, T>();
            build.Services.TryAddScoped<ConsistencyMessageManager>();
            return build;
        }
    }
}