using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Abstractions.ModelBinding;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        /// <param name="setupAction">An action to configure the <see cref="CapOptions"/>.</param>
        /// <returns>An <see cref="CapBuilder"/> for application services.</returns>
        public static CapBuilder AddCap(
            this IServiceCollection services,
            Action<CapOptions> setupAction)
        {
            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

            services.TryAddSingleton<CapMarkerService>();
            services.Configure(setupAction);

            AddSubscribeServices(services);

            services.TryAddSingleton<IConsumerServiceSelector, DefaultConsumerServiceSelector>();
            services.TryAddSingleton<IModelBinder, DefaultModelBinder>();
            services.TryAddSingleton<IConsumerInvokerFactory, ConsumerInvokerFactory>();
            services.TryAddSingleton<MethodMatcherCache>();

            services.AddSingleton<IProcessingServer, ConsumerHandler>();
            services.AddSingleton<IProcessingServer, CapProcessingServer>();
            services.AddSingleton<IBootstrapper, DefaultBootstrapper>();
            services.AddSingleton<IStateChanger, StateChanger>();

            //Processors
            services.AddTransient<PublishQueuer>();
            services.AddTransient<SubscribeQueuer>();
            services.AddTransient<FailedJobProcessor>();
            services.AddTransient<IDispatcher, DefaultDispatcher>();

            //Executors
            services.AddSingleton<IQueueExecutorFactory, QueueExecutorFactory>();
            services.AddSingleton<IQueueExecutor, SubscibeQueueExecutor>();

            //Options and extension service
            var options = new CapOptions();
            setupAction(options);
            foreach (var serviceExtension in options.Extensions)
            {
                serviceExtension.AddServices(services);
            }          
            services.AddSingleton(options);

            return new CapBuilder(services);
        }

        private static void AddSubscribeServices(IServiceCollection services)
        {
            var consumerListenerServices = new Dictionary<Type, Type>();
            foreach (var rejectedServices in services)
            {
                if (rejectedServices.ImplementationType != null
                    && typeof(ICapSubscribe).IsAssignableFrom(rejectedServices.ImplementationType))

                    consumerListenerServices.Add(typeof(ICapSubscribe), rejectedServices.ImplementationType);
            }

            foreach (var service in consumerListenerServices)
            {
                services.AddSingleton(service.Key, service.Value);
            }

            var types = Assembly.GetEntryAssembly().ExportedTypes;
            foreach (var type in types)
            {
                if (Helper.IsController(type.GetTypeInfo()))
                {
                    services.AddSingleton(typeof(object), type);
                }
            }
        }
    }
}