// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Filter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Used to verify cap service was called on a ServiceCollection
    /// </summary>
    public class CapMarkerService
    {
    }

    /// <summary>
    /// Used to verify cap storage extension was added on a ServiceCollection
    /// </summary>
    public class CapStorageMarkerService
    {
    }

    /// <summary>
    /// Used to verify cap message queue extension was added on a ServiceCollection
    /// </summary>
    public class CapMessageQueueMakerService
    {
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

        public CapBuilder AddSubscribeFilter<T>() where T : class, ISubscribeFilter
        {
            Services.TryAddScoped<ISubscribeFilter, T>();
            return this;
        }
    }
}