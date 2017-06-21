using System;
using Cap.Consistency.Job;
using Cap.Consistency.Store;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Used to verify Consistency service was called on a ServiceCollection
    /// </summary>
    public class ConsistencyMarkerService { }

    public class ConsistencyBuilder
    {
        public ConsistencyBuilder(IServiceCollection services) {
            Services = services;
        }

        public IServiceCollection Services { get; private set; }

        private ConsistencyBuilder AddScoped(Type serviceType, Type concreteType) {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }

        private ConsistencyBuilder AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService {
            Services.AddSingleton<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adds an <see cref="IConsistencyMessageStore"/> .
        /// </summary>
        /// <typeparam name="T">The type for the <see cref="IConsistencyMessageStore"/> to add. </typeparam>
        /// <returns>The current <see cref="ConsistencyBuilder"/> instance.</returns>
        public virtual ConsistencyBuilder AddMessageStore<T>()
            where T : class, IConsistencyMessageStore {

            return AddScoped(typeof(IConsistencyMessageStore), typeof(T));
        }

        public virtual ConsistencyBuilder AddJobs<T>()
            where T : class, IJob {

            return AddSingleton<IJob, T>();
        }
    }
}