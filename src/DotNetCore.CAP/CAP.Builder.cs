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
    /// Allows fine grained configuration of CAP services.
    /// </summary>
    public class CapBuilder
    {
        public CapBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where MVC services are configured.
        /// </summary>
        public IServiceCollection Services { get; private set; }

        /// <summary>
        /// Adds a scoped service of the type specified in serviceType with an implementation
        /// </summary>
        private CapBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }

        /// <summary>
        /// Add an <see cref="ICapPublisher"/>.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        public virtual CapBuilder AddProducerService<T>()
            where T : class, ICapPublisher
        {
            return AddScoped(typeof(ICapPublisher), typeof(T));
        }
    }
}