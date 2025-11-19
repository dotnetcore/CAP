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
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering and configuring CAP (Consistency And Partition) services
/// in a <see cref="IServiceCollection"/> dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers and configures all CAP services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// This method performs the following registrations:
    /// <list type="bullet">
    /// <item><description>Core services: Message publisher, consumer selector, subscription invoker, and method matcher cache.</description></item>
    /// <item><description>Message processors: Retry processor, transport check processor, delayed message processor, and message collector.</description></item>
    /// <item><description>Message transport: Message sender and default JSON serializer.</description></item>
    /// <item><description>Processing servers: Consumer registration server, dispatcher, and main CAP processing server.</description></item>
    /// <item><description>Bootstrapper and hosted service for application startup and lifecycle management.</description></item>
    /// <item><description>Extensions: Any configured storage and transport extensions (registered via <see cref="CapOptions.RegisterExtension"/>).</description></item>
    /// </list>
    /// All core services are registered with singleton lifetime to ensure consistency across the application.
    /// Storage and transport extensions must be added before calling this method (typically through AddCap callback).
    /// </remarks>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> where CAP services will be registered.
    /// This collection represents the application's dependency injection container.
    /// </param>
    /// <param name="setupAction">
    /// A delegate that configures the <see cref="CapOptions"/> settings for CAP.
    /// This action is invoked to customize behavior such as message expiration, retry policies, concurrency settings,
    /// and to register storage and transport extensions.
    /// Use this to call <c>UseRabbitMQ()</c>, <c>UseSqlServer()</c>, and other extension methods.
    /// </param>
    /// <returns>
    /// A <see cref="CapBuilder"/> instance that provides a fluent API for additional CAP configuration,
    /// such as registering subscriber filters and custom subscriber assembly scanning.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="setupAction"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddCap(options =>
    /// {
    ///     // Configure options
    ///     options.SucceedMessageExpiredAfter = 24 * 3600;
    ///     options.FailedRetryCount = 50;
    ///     
    ///     // Register storage backend
    ///     options.UseSqlServer("your_connection_string");
    ///     
    ///     // Register message transport
    ///     options.UseRabbitMQ(rabbitMqOptions =>
    ///     {
    ///         rabbitMqOptions.HostName = "localhost";
    ///         rabbitMqOptions.Port = 5672;
    ///     });
    /// })
    /// .AddSubscribeFilter&lt;LoggingFilter&gt;()
    /// .AddSubscriberAssembly(typeof(MyCapHandlers));
    /// </code>
    /// </example>
    public static CapBuilder AddCap(this IServiceCollection services, Action<CapOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);

        services.AddSingleton(_ => services);
        services.TryAddSingleton(new CapMarkerService("CAP"));
        services.TryAddSingleton<ISnowflakeId, SnowflakeId>();
        services.TryAddSingleton<ICapPublisher, CapPublisher>();

        services.TryAddSingleton<IConsumerServiceSelector, ConsumerServiceSelector>();
        services.TryAddSingleton<ISubscribeInvoker, SubscribeInvoker>();
        services.TryAddSingleton<MethodMatcherCache>();

        services.TryAddSingleton<IConsumerRegister, ConsumerRegister>();

        //Processors
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, IDispatcher>(sp => sp.GetRequiredService<IDispatcher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, IConsumerRegister>(sp => sp.GetRequiredService<IConsumerRegister>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, CapProcessingServer>());

        //Queue's message processor
        services.TryAddSingleton<MessageNeedToRetryProcessor>();
        services.TryAddSingleton<TransportCheckProcessor>();
        services.TryAddSingleton<MessageDelayedProcessor>();
        services.TryAddSingleton<CollectorProcessor>();

        //Sender
        services.TryAddSingleton<IMessageSender, MessageSender>();

        services.TryAddSingleton<ISerializer, JsonUtf8Serializer>();

        // Warning: IPublishMessageSender need to inject at extension project. 
        services.TryAddSingleton<ISubscribeExecutor, SubscribeExecutor>();

        //Options and extension service
        var options = new CapOptions();
        setupAction(options);

        services.TryAddSingleton<IDispatcher, Dispatcher>();

        foreach (var serviceExtension in options.Extensions)
            serviceExtension.AddServices(services);

        services.Configure(setupAction);

        //Startup and Hosted 
        services.AddSingleton<Bootstrapper>();
        services.AddHostedService(sp => sp.GetRequiredService<Bootstrapper>());
        services.AddSingleton<IBootstrapper>(sp => sp.GetRequiredService<Bootstrapper>());

        return new CapBuilder(services);
    }
}
