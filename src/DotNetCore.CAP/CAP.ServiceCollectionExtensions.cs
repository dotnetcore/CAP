// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection" /> for configuring consistence services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        internal static IServiceCollection ServiceCollection;

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

            ServiceCollection = services;

            services.TryAddSingleton<CapMarkerService>();

            services.TryAddSingleton<ICapPublisher, CapPublisher>();

            services.TryAddSingleton<IConsumerServiceSelector, ConsumerServiceSelector>();
            services.TryAddSingleton<ISubscribeInvoker, SubscribeInvoker>();
            services.TryAddSingleton<MethodMatcherCache>();

            services.TryAddSingleton<IConsumerRegister, ConsumerRegister>();

            //Processors
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, CapProcessingServer>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, ConsumerRegister>());

            //Queue's message processor
            services.TryAddSingleton<MessageNeedToRetryProcessor>();
            services.TryAddSingleton<TransportCheckProcessor>();
            services.TryAddSingleton<CollectorProcessor>();

            //Sender and Executors   
            services.TryAddSingleton<IMessageSender, MessageSender>();
            services.TryAddSingleton<IDispatcher, Dispatcher>();

            services.TryAddSingleton<ISerializer, JsonUtf8Serializer>();

            // Warning: IPublishMessageSender need to inject at extension project. 
            services.TryAddSingleton<ISubscribeDispatcher, SubscribeDispatcher>();

            //Options and extension service
            var options = new CapOptions();
            setupAction(options);
            foreach (var serviceExtension in options.Extensions)
            {
                serviceExtension.AddServices(services);
            }
            services.Configure(setupAction);

            //Startup and Hosted 
            services.AddSingleton<IBootstrapper, Bootstrapper>();
            services.AddHostedService<Bootstrapper>();

            return new CapBuilder(services);
        }
    }
}