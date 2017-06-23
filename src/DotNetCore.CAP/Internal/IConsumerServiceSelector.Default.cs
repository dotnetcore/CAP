using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    ///  A default <see cref="IConsumerServiceSelector"/> implementation.
    /// </summary>
    public class DefaultConsumerServiceSelector : IConsumerServiceSelector
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new <see cref="DefaultConsumerServiceSelector"/>.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public DefaultConsumerServiceSelector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="executeDescriptor"></param>
        /// <returns></returns>
        public ConsumerExecutorDescriptor SelectBestCandidate(string key,
            IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            return executeDescriptor.FirstOrDefault(x => x.Attribute.Name == key);
        }

        public IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(CapStartContext context)
        {
            var consumerServices = context.ServiceProvider.GetServices<IConsumerService>();

            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();
            foreach (var service in consumerServices)
            {
                var typeInfo = service.GetType().GetTypeInfo();
                if (!typeof(IConsumerService).GetTypeInfo().IsAssignableFrom(typeInfo))
                {
                    continue;
                }

                foreach (var method in typeInfo.DeclaredMethods)
                {
                    var topicAttr = method.GetCustomAttribute<TopicAttribute>(true);
                    if (topicAttr == null) continue;

                    executorDescriptorList.Add(InitDescriptor(topicAttr, method, typeInfo));
                }
            }

            return executorDescriptorList;
        }

        private ConsumerExecutorDescriptor InitDescriptor(
            TopicAttribute attr,
            MethodInfo methodInfo,
            TypeInfo implType)
        {
            var descriptor = new ConsumerExecutorDescriptor()
            {
                Attribute = attr,
                MethodInfo = methodInfo,
                ImplTypeInfo = implType
            };

            return descriptor;
        }
    }
}