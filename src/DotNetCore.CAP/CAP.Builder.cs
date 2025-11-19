// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable UnusedMember.Global

namespace DotNetCore.CAP;

/// <summary>
/// A marker service used internally to verify that the CAP service has been registered on a <see cref="IServiceCollection"/>.
/// This service is registered when <c>AddCap()</c> is called during dependency injection setup.
/// </summary>
public class CapMarkerService
{
    /// <summary>
    /// Gets or sets the name identifier for the CAP service.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the CAP assembly.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapMarkerService"/> class with the specified name.
    /// Automatically retrieves and stores the CAP assembly version information.
    /// </summary>
    /// <param name="name">The name identifier for the CAP service.</param>
    public CapMarkerService(string name)
    {
        Name = name;

        try
        {
            Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion!;
        }
        catch
        {
            Version = "N/A";
        }
    }
}

/// <summary>
/// A marker service used internally to verify that a CAP storage extension (e.g., SQL Server, PostgreSQL, MySQL, MongoDB) 
/// has been registered on a <see cref="IServiceCollection"/>.
/// </summary>
public class CapStorageMarkerService
{
    /// <summary>
    /// Gets or sets the name identifier for the storage extension (e.g., "SqlServer", "PostgreSql", "MySql").
    /// </summary>
    public string Name { get; set; }

    //public IDictionary<string, string> MetaData { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapStorageMarkerService"/> class with the specified storage name.
    /// </summary>
    /// <param name="name">The name identifier for the storage extension.</param>
    public CapStorageMarkerService(string name)
    {
        Name = name;
    }
}

/// <summary>
/// A marker service used internally to verify that a CAP message transport extension (e.g., RabbitMQ, Kafka, Azure Service Bus, NATS) 
/// has been registered on a <see cref="IServiceCollection"/>.
/// </summary>
public class CapMessageQueueMakerService
{
    /// <summary>
    /// Gets or sets the name identifier for the message transport extension (e.g., "RabbitMQ", "Kafka", "AzureServiceBus").
    /// </summary>
    public string Name { get; set; }

    //public IDictionary<string, object> MetaData { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapMessageQueueMakerService"/> class with the specified message queue name.
    /// </summary>
    /// <param name="name">The name identifier for the message transport extension.</param>
    public CapMessageQueueMakerService(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Provides a fluent API for fine-grained configuration of CAP services within a dependency injection container.
/// This builder allows registration of subscriber filters, custom subscriber assembly scanning, and other CAP extensions.
/// </summary>
/// <remarks>
/// The <see cref="CapBuilder"/> is typically obtained through the <c>AddCap()</c> extension method on <see cref="IServiceCollection"/>,
/// enabling a fluent configuration experience for CAP setup. All builder methods return the builder instance to support method chaining.
/// </remarks>
public sealed class CapBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CapBuilder"/> class with the specified service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> where CAP services are being configured.</param>
    public CapBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where CAP services are registered and configured.
    /// </summary>
    /// <remarks>
    /// This collection is used by all builder methods to register necessary services, filters, and extensions
    /// in the application's dependency injection container.
    /// </remarks>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers a subscriber filter that will be applied to all subscriber method executions.
    /// Filters can be used for cross-cutting concerns such as logging, error handling, and transaction management.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the filter to register. Must implement <see cref="ISubscribeFilter"/> and be instantiable.
    /// The filter is registered with a scoped lifetime, meaning a new instance is created per request/scope.
    /// </typeparam>
    /// <returns>The current <see cref="CapBuilder"/> instance to support fluent method chaining.</returns>
    /// <remarks>
    /// Multiple filters can be registered by calling this method multiple times. Filters are executed in the order
    /// they are registered, allowing for layered processing of subscriber messages.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddCap(options =>
    /// {
    ///     options.UseRabbitMQ(r => r.HostName = "localhost");
    ///     options.UseSqlServer("connection_string");
    /// })
    /// .AddSubscribeFilter&lt;LoggingFilter&gt;()
    /// .AddSubscribeFilter&lt;ExceptionHandlingFilter&gt;();
    /// </code>
    /// </example>
    public CapBuilder AddSubscribeFilter<T>() where T : class, ISubscribeFilter
    {
        Services.TryAddScoped<ISubscribeFilter, T>();
        return this;
    }

    /// <summary>
    /// Registers subscriber methods from the specified assemblies.
    /// This method scans the provided assemblies for classes and methods decorated with CAP subscriber attributes.
    /// </summary>
    /// <param name="assemblies">
    /// An array of <see cref="Assembly"/> instances to scan for subscriber implementations.
    /// </param>
    /// <returns>The current <see cref="CapBuilder"/> instance to support fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assemblies"/> is null.</exception>
    /// <remarks>
    /// This method replaces the default subscriber selector with a custom one that scans the specified assemblies.
    /// By default, CAP scans all loaded assemblies; this method allows you to restrict scanning to specific assemblies
    /// for performance optimization or explicit control over subscriber discovery.
    /// </remarks>
    /// <example>
    /// <code>
    /// var capAssembly = typeof(MyCapHandlers).Assembly;
    /// services.AddCap(options => { })
    ///     .AddSubscriberAssembly(capAssembly);
    /// </code>
    /// </example>
    public CapBuilder AddSubscriberAssembly(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        Services.Replace(new ServiceDescriptor(typeof(IConsumerServiceSelector),
            x => new AssemblyConsumerServiceSelector(x, assemblies),
            ServiceLifetime.Singleton));

        return this;
    }

    /// <summary>
    /// Registers subscriber methods from the assemblies containing the specified marker types.
    /// This is a convenience overload that extracts the assemblies from the provided types and delegates to <see cref="AddSubscriberAssembly(Assembly[])"/>.
    /// </summary>
    /// <param name="handlerAssemblyMarkerTypes">
    /// An array of marker types (typically classes in the target assemblies) whose containing assemblies will be scanned for subscribers.
    /// This approach allows type-safe specification of which assemblies to include without directly referencing <see cref="Assembly"/> objects.
    /// </param>
    /// <returns>The current <see cref="CapBuilder"/> instance to support fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handlerAssemblyMarkerTypes"/> is null.</exception>
    /// <remarks>
    /// This method is particularly useful in multi-project scenarios where you have separate projects for handlers
    /// but want to avoid direct assembly references. Simply pass a type from each assembly, and the method will
    /// automatically extract and scan those assemblies.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Assuming MyCapHandlers is a class in the assembly containing subscriber implementations
    /// services.AddCap(options => { })
    ///     .AddSubscriberAssembly(typeof(MyCapHandlers));
    /// </code>
    /// </example>
    public CapBuilder AddSubscriberAssembly(params Type[] handlerAssemblyMarkerTypes)
    {
        ArgumentNullException.ThrowIfNull(handlerAssemblyMarkerTypes);

        AddSubscriberAssembly(handlerAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly).ToArray());

        return this;
    }
}