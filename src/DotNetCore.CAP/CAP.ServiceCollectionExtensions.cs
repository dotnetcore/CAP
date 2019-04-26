// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        public static CapBuilder AddCap(this IServiceCollection services, Action<CapOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.TryAddSingleton<CapMarkerService>();

            //Consumer service
            AddSubscribeServices(services);

            //Serializer and model binder
            services.TryAddSingleton<IContentSerializer, JsonContentSerializer>();
            services.TryAddSingleton<IMessagePacker, DefaultMessagePacker>();
            services.TryAddSingleton<IConsumerServiceSelector, DefaultConsumerServiceSelector>();
            services.TryAddSingleton<IModelBinderFactory, ModelBinderFactory>();

            services.TryAddSingleton<ICallbackMessageSender, CallbackMessageSender>();
            services.TryAddSingleton<IConsumerInvokerFactory, ConsumerInvokerFactory>();
            services.TryAddSingleton<MethodMatcherCache>();

            //Processors
            services.TryAddSingleton<IConsumerRegister, ConsumerRegister>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, CapProcessingServer>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, ConsumerRegister>());
            services.TryAddSingleton<IStateChanger, StateChanger>();

            //Queue's message processor
            services.TryAddSingleton<NeedRetryMessageProcessor>();
            services.TryAddSingleton<TransportCheckProcessor>();

            //Sender and Executors   
            services.TryAddSingleton<IDispatcher, Dispatcher>();
            // Warning: IPublishMessageSender need to inject at extension project. 
            services.TryAddSingleton<ISubscriberExecutor, DefaultSubscriberExecutor>();

            //Options and extension service
            var options = new CapOptions();
            setupAction(options);
            foreach (var serviceExtension in options.Extensions)
            {
                serviceExtension.AddServices(services);
            }
            services.AddSingleton(options);

            //Startup and Middleware
            services.AddTransient<IHostedService, DefaultBootstrapper>();
            services.AddTransient<IStartupFilter, CapStartupFilter>();

            return new CapBuilder(services);
        }

        private static void AddSubscribeServices(IServiceCollection services)
        {
            var consumerListenerServices = new List<KeyValuePair<Type, Type>>();
            foreach (var rejectedServices in services)
            {
                if (rejectedServices.ImplementationType != null
                    && typeof(ICapSubscribe).IsAssignableFrom(rejectedServices.ImplementationType))
                {
                    consumerListenerServices.Add(new KeyValuePair<Type, Type>(typeof(ICapSubscribe),
                        rejectedServices.ImplementationType));
                }
            }

            foreach (var service in consumerListenerServices)
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(service.Key, service.Value));
            }
        }
    }
}