using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Job;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Used to verify Consistency service was called on a ServiceCollection
    /// </summary>
    public class CapMarkerService { }

    public class CapBuilder
    {
        public CapBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; private set; }

        private CapBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }

        private CapBuilder AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            Services.AddSingleton<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="ICapMessageStore"/> .
        /// </summary>
        /// <typeparam name="T">The type for the <see cref="ICapMessageStore"/> to add. </typeparam>
        /// <returns>The current <see cref="CapBuilder"/> instance.</returns>
        public virtual CapBuilder AddMessageStore<T>()
            where T : class, ICapMessageStore
        {
            return AddScoped(typeof(ICapMessageStore), typeof(T));
        }

        public virtual CapBuilder AddJobs<T>()
            where T : class, IJob
        {
            return AddSingleton<IJob, T>();
        }

        public virtual CapBuilder AddProducerClient<T>()
            where T : class, ICapProducerService
        {
            return AddScoped(typeof(ICapProducerService), typeof(T));
        }
    }
}