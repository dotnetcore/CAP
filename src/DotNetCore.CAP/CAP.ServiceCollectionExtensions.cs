using System;
using System.Collections.Generic;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection" /> for configuring consistence services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures the consistence services for the consistency.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">An action to configure the <see cref="CapOptions" />.</param>
        /// <returns>An <see cref="CapBuilder" /> for application services.</returns>
        public static CapBuilder AddCap(
            this IServiceCollection services,
            Action<CapOptions> setupAction)
        {
            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

            services.TryAddSingleton<CapMarkerService>();
            services.Configure(setupAction);

            AddSubscribeServices(services);

            //Serializer and model binder
            services.TryAddSingleton<IContentSerializer, JsonContentSerializer>();
            services.TryAddSingleton<IMessagePacker, DefaultMessagePacker>();
            services.TryAddSingleton<IConsumerServiceSelector, DefaultConsumerServiceSelector>();
            services.TryAddSingleton<IModelBinderFactory, ModelBinderFactory>();

            services.TryAddSingleton<ICallbackMessageSender, CallbackMessageSender>();
            services.TryAddSingleton<IConsumerInvokerFactory, ConsumerInvokerFactory>();
            services.TryAddSingleton<MethodMatcherCache>();

            //Bootstrapper and Processors
            services.AddSingleton<IProcessingServer, ConsumerHandler>();
            services.AddSingleton<IProcessingServer, CapProcessingServer>();
            services.AddSingleton<IBootstrapper, DefaultBootstrapper>();
            services.AddSingleton<IStateChanger, StateChanger>();

            //Queue's message processor
            services.AddTransient<PublishQueuer>();
            services.AddTransient<SubscribeQueuer>();
            services.AddTransient<FailedProcessor>();
            services.AddTransient<IDispatcher, DefaultDispatcher>();

            //Executors
            services.AddSingleton<IQueueExecutorFactory, QueueExecutorFactory>();
            services.AddSingleton<IQueueExecutor, SubscribeQueueExecutor>();
            services.TryAddSingleton<ISubscriberExecutor, DefaultSubscriberExecutor>();

            //Options and extension service
            var options = new CapOptions();
            setupAction(options);
            foreach (var serviceExtension in options.Extensions)
                serviceExtension.AddServices(services);
            services.AddSingleton(options);

            return new CapBuilder(services);
        }

        private static void AddSubscribeServices(IServiceCollection services)
        {
            var consumerListenerServices = new List<KeyValuePair<Type, Type>>();
            foreach (var rejectedServices in services)
                if (rejectedServices.ImplementationType != null
                    && typeof(ICapSubscribe).IsAssignableFrom(rejectedServices.ImplementationType))
                    consumerListenerServices.Add(new KeyValuePair<Type, Type>(typeof(ICapSubscribe),
                        rejectedServices.ImplementationType));

            foreach (var service in consumerListenerServices)
                services.AddTransient(service.Key, service.Value);
        }
    }
}