// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

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

        /// <summary>
        /// Add an <see cref="ICapPublisher" />.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        public CapBuilder AddProducerService<T>()
            where T : class, ICapPublisher
        {
            return AddScoped(typeof(ICapPublisher), typeof(T));
        }

        /// <summary>
        /// Adds a scoped service of the type specified in serviceType with an implementation
        /// </summary>
        private CapBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }

        /// <summary>
        /// Adds a singleton service of the type specified in serviceType with an implementation
        /// </summary>
        private CapBuilder AddSingleton(Type serviceType, Type concreteType)
        {
            Services.AddSingleton(serviceType, concreteType);
            return this;
        }
    }
}