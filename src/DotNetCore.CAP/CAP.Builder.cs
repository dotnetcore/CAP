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
/// Used to verify cap service was called on a ServiceCollection
/// </summary>
public class CapMarkerService
{
    public string Name { get; set; }

    public string Version { get; set; }

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
/// Used to verify cap storage extension was added on a ServiceCollection
/// </summary>
public class CapStorageMarkerService
{
    public string Name { get; set; }

    //public IDictionary<string, string> MetaData { get; private set; }

    public CapStorageMarkerService(string name)
    {
        Name = name;
        //MetaData = new Dictionary<string, string>();
    }

    //public void AddMetaData(string key, string value)
    //{
    //    MetaData.Add(key, value);
    //}
}

/// <summary>
/// Used to verify cap message queue extension was added on a ServiceCollection
/// </summary>
public class CapMessageQueueMakerService
{
    public string Name { get; set; }

    //public IDictionary<string, object> MetaData { get; private set; }

    public CapMessageQueueMakerService(string name)
    {
        Name = name;
        //MetaData = new Dictionary<string, object>();
    }

    //public void AddMetaData(string key, string value)
    //{
    //    MetaData.Add(key, value);
    //}
}

/// <summary>
/// Allows fine grained configuration of CAP services.
/// </summary>
public sealed class CapBuilder
{
    public CapBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Gets the <see cref="IServiceCollection" /> where MVC services are configured.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers subscribers filter.
    /// </summary>
    /// <typeparam name="T">Type of filter</typeparam>
    public CapBuilder AddSubscribeFilter<T>() where T : class, ISubscribeFilter
    {
        Services.TryAddScoped<ISubscribeFilter, T>();
        return this;
    }

    /// <summary>
    /// Registers subscribers from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan subscriber</param>
    public CapBuilder AddSubscriberAssembly(params Assembly[] assemblies)
    {
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        Services.Replace(new ServiceDescriptor(typeof(IConsumerServiceSelector),
            x => new AssemblyConsumerServiceSelector(x, assemblies),
            ServiceLifetime.Singleton));

        return this;
    }

    /// <summary>
    /// Registers subscribers from the specified types.
    /// </summary>
    /// <param name="handlerAssemblyMarkerTypes"></param>
    public CapBuilder AddSubscriberAssembly(params Type[] handlerAssemblyMarkerTypes)
    {
        if (handlerAssemblyMarkerTypes == null) throw new ArgumentNullException(nameof(handlerAssemblyMarkerTypes));

        AddSubscriberAssembly(handlerAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly).ToArray());

        return this;
    }
}