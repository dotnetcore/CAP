using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cap.Consistency
{
    /// <summary>
    /// Creates a new instance of <see cref="ConsistenceBuilder"/>.
    /// </summary>
    /// <param name="message">The <see cref="Type"/> to use for the message.</param>
    /// <param name="services">The <see cref="IServiceCollection"/> to attach to.</param>
    public class ConsistenceBuilder
    {
        public ConsistenceBuilder(Type message, IServiceCollection service) {
            MessageType = message;
            Services = service;
        }

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> services are attached to.
        /// </summary>
        /// <value>
        /// The <see cref="IServiceCollection"/> services are attached to.
        /// </value>
        public IServiceCollection Services { get; private set; }

        /// <summary>
        /// Gets the <see cref="Type"/> used for messages.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> used for messages.
        /// </value>
        public Type MessageType { get; private set; }

        /// <summary>
        /// Adds a <see cref="IRoleStore{TRole}"/> for the <seealso cref="RoleType"/>.
        /// </summary>
        /// <typeparam name="T">The role type held in the store.</typeparam>
        /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
        public virtual ConsistenceBuilder AddMessageStore<T>() where T : class {
            return AddScoped(typeof(IConsistentMessageStore<>).MakeGenericType(MessageType), typeof(T));
        }

        private ConsistenceBuilder AddScoped(Type serviceType, Type concreteType) {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }
    }
}