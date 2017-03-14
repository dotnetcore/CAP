using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Cap.Consistency
{
    /// <summary>
    /// Helper functions for configuring consistency services.
    /// </summary>
    public class ConsistencyBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="ConsistencyBuilder"/>.
        /// </summary>
        /// <param name="message">The <see cref="Type"/> to use for the message.</param>
        /// <param name="service">The <see cref="IServiceCollection"/> to attach to.</param>
        public ConsistencyBuilder(Type message, IServiceCollection service) {
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
        /// Adds a <see cref="IConsistencyMessageStore{TMessage}"/> for the <seealso cref="MessageType"/>.
        /// </summary>
        /// <typeparam name="T">The role type held in the store.</typeparam>
        /// <returns>The current <see cref="ConsistencyBuilder"/> instance.</returns>
        public virtual ConsistencyBuilder AddMessageStore<T>() where T : class {
            return AddScoped(typeof(IConsistencyMessageStore<>).MakeGenericType(MessageType), typeof(T));
        }

        public virtual ConsistencyBuilder AddMessageMethodTable() {
            var provider = Services.BuildServiceProvider();

            var finder = provider.GetRequiredService<QMessageFinder>();
            finder.GetQMessageMethods(Services);               
            return null;
            // Services.AddSingleton(serviceType, concreteType);
            // return Add(typeof(IConsistencyMessageStore<>).MakeGenericType(MessageType), typeof(T));
        }

        /// <summary>
        /// Adds a <see cref="ConsistencyMessageManager{TUser}"/> for the <seealso cref="MessageType"/>.
        /// </summary>
        /// <typeparam name="TMessageManager">The type of the message manager to add.</typeparam>
        /// <returns>The current <see cref="ConsistencyBuilder"/> instance.</returns>
        public virtual ConsistencyBuilder AddConsistencyMessageManager<TMessageManager>() where TMessageManager : class {
            var messageManagerType = typeof(ConsistencyMessageManager<>).MakeGenericType(MessageType);
            var customType = typeof(TMessageManager);
            if (messageManagerType == customType ||
                !messageManagerType.GetTypeInfo().IsAssignableFrom(customType.GetTypeInfo())) {
                throw new InvalidOperationException($"Type {customType.Name} must be derive from ConsistencyMessageManager<{MessageType.Name}>");
            }
            Services.AddScoped(customType, services => services.GetRequiredService(messageManagerType));
            return AddScoped(messageManagerType, customType);
        }

        private ConsistencyBuilder AddScoped(Type serviceType, Type concreteType) {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }
    }
}