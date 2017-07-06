﻿using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Abstractions.ModelBinding;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection"/> for configuring consistence services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures the CAP services for the consitence.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <returns>An <see cref="CapBuilder"/> for application services.</returns>
        public static CapBuilder AddCap(this IServiceCollection services)
        {
            services.AddCap(x => new CapOptions());

            return new CapBuilder(services);
        }

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
            services.TryAddSingleton<CapMarkerService>();
            services.Configure(setupAction);

            AddConsumerServices(services);

            services.TryAddSingleton<IConsumerServiceSelector, DefaultConsumerServiceSelector>();
            services.TryAddSingleton<IModelBinder, DefaultModelBinder>();
            services.TryAddSingleton<IConsumerInvokerFactory, ConsumerInvokerFactory>();
            services.TryAddSingleton<MethodMatcherCache>();

            services.AddSingleton<IProcessingServer, ConsumerHandler>();
            services.AddSingleton<IProcessingServer, JobProcessingServer>();
            services.AddSingleton<IBootstrapper, DefaultBootstrapper>();

            services.TryAddTransient<IJobProcessor, CronJobProcessor>();
            services.TryAddSingleton<IJob, CapJob>();
            services.TryAddTransient<DefaultCronJobRegistry>();

            services.TryAddScoped<ICapProducerService, DefaultProducerService>();

            return new CapBuilder(services);
        }

        private static void AddConsumerServices(IServiceCollection services)
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