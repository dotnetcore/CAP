using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// A default <see cref="T:DotNetCore.CAP.Abstractions.IConsumerServiceSelector" /> implementation.
    /// </summary>
    internal class DefaultConsumerServiceSelector : IConsumerServiceSelector
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new <see cref="DefaultConsumerServiceSelector" />.
        /// </summary>
        public DefaultConsumerServiceSelector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Selects the best <see cref="ConsumerExecutorDescriptor" /> candidate from <paramref name="executeDescriptor" /> for the
        /// current message associated.
        /// </summary>
        public ConsumerExecutorDescriptor SelectBestCandidate(string key,
            IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            return executeDescriptor.FirstOrDefault(x => x.Attribute.Name == key);
        }

        public IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates()
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            executorDescriptorList.AddRange(FindConsumersFromInterfaceTypes(_serviceProvider));

            executorDescriptorList.AddRange(FindConsumersFromControllerTypes());

            executorDescriptorList.AddRange(FindCustomConsumersFromInterfaceTypes(_serviceProvider));

            return executorDescriptorList;
        }

        private static IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(
            IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            using (var scoped = provider.CreateScope())
            {
                var scopedProvider = scoped.ServiceProvider;
                var consumerServices = scopedProvider.GetServices<ICapSubscribe>();
                foreach (var service in consumerServices)
                {
                    var typeInfo = service.GetType().GetTypeInfo();
                    if (!typeof(ICapSubscribe).GetTypeInfo().IsAssignableFrom(typeInfo))
                        continue;

                    executorDescriptorList.AddRange(GetTopicAttributesDescription(typeInfo));
                }
                return executorDescriptorList;
            }
        }

        private static IEnumerable<ConsumerExecutorDescriptor> FindCustomConsumersFromInterfaceTypes(IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            using (var scoped = provider.CreateScope())
            {
                var scopedProvider = scoped.ServiceProvider;
                var customConsumerServices = scopedProvider.GetServices<ICapCustomSubscribe>();
                var configuration = scopedProvider.GetService<IConfiguration>();
                foreach (var service in customConsumerServices)
                {
                    var typeInfo = service.GetType().GetTypeInfo();
                    if (!typeof(ICapCustomSubscribe).GetTypeInfo().IsAssignableFrom(typeInfo)) continue;

                    executorDescriptorList.AddRange(GetTopicConfigurationDescription(typeInfo, configuration));
                }

                return executorDescriptorList;
            }
        }



        private static IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromControllerTypes()
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            var types = Assembly.GetEntryAssembly().ExportedTypes;
            foreach (var type in types)
            {
                var typeInfo = type.GetTypeInfo();
                if (Helper.IsController(typeInfo))
                    executorDescriptorList.AddRange(GetTopicAttributesDescription(typeInfo));
            }

            return executorDescriptorList;
        }

        private static IEnumerable<ConsumerExecutorDescriptor> GetTopicConfigurationDescription(TypeInfo typeInfo, IConfiguration configuration)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();
            var topics = configuration.GetSection("Topics").GetChildren();
            var method = typeInfo.DeclaredMethods.ToArray()[0];
            foreach (var topic in topics)
            {
                var t = topic.Key.Split(';').ToArray();
                var attr = new TopicName(t.Last());
                attr.Group = t.First();
                yield return InitDescriptor(attr, method, typeInfo);
            }
        }

        private static IEnumerable<ConsumerExecutorDescriptor> GetTopicAttributesDescription(TypeInfo typeInfo)
        {
            foreach (var method in typeInfo.DeclaredMethods)
            {
                var topicAttr = method.GetCustomAttributes<TopicAttribute>(true);

                var topicAttributes = topicAttr as IList<TopicAttribute> ?? topicAttr.ToList();

                if (!topicAttributes.Any()) continue;

                foreach (var attr in topicAttributes)
                    yield return InitDescriptor(attr, method, typeInfo);
            }
        }

        private static ConsumerExecutorDescriptor InitDescriptor(
            TopicAttribute attr,
            MethodInfo methodInfo,
            TypeInfo implType)
        {
            var descriptor = new ConsumerExecutorDescriptor
            {
                Attribute = attr,
                MethodInfo = methodInfo,
                ImplTypeInfo = implType
            };

            return descriptor;
        }
    }
}