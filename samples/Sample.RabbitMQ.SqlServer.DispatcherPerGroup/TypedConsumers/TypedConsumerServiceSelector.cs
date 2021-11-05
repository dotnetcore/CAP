using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers
{
    internal class TypedConsumerServiceSelector : ConsumerServiceSelector
    {
        private readonly CapOptions _capOptions;

        public TypedConsumerServiceSelector(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _capOptions = serviceProvider.GetRequiredService<IOptions<CapOptions>>().Value;
        }

        protected override IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>(30);

            using var scoped = provider.CreateScope();
            var consumerServices = scoped.ServiceProvider.GetServices<QueueHandler>();
            foreach (var service in consumerServices)
            {
                var typeInfo = service.GetType().GetTypeInfo();
                if (!typeof(QueueHandler).GetTypeInfo().IsAssignableFrom(typeInfo))
                {
                    continue;
                }

                executorDescriptorList.AddRange(GetMyDescription(typeInfo));
            }

            return executorDescriptorList;
        }

        private IEnumerable<ConsumerExecutorDescriptor> GetMyDescription(TypeInfo typeInfo)
        {
            var method = typeInfo.DeclaredMethods.FirstOrDefault(x => x.Name == "Handle");
            if (method == null) yield break;

            var topicAttr = typeInfo.GetCustomAttributes<QueueHandlerTopicAttribute>(true);
            var topicAttributes = topicAttr as IList<QueueHandlerTopicAttribute> ?? topicAttr.ToList();

            if (topicAttributes.Count == 0) yield break;

            foreach (var attr in topicAttributes)
            {
                var topic = attr.Topic == null
                    ? _capOptions.DefaultGroupName + "." + _capOptions.Version
                    : attr.Topic + "." + _capOptions.Version;

                if (!string.IsNullOrEmpty(_capOptions.GroupNamePrefix))
                {
                    topic = $"{_capOptions.GroupNamePrefix}.{topic}";
                }

                var parameters = method.GetParameters().Select(p => new ParameterDescriptor
                {
                    Name = p.Name,
                    ParameterType = p.ParameterType,
                    IsFromCap = p.GetCustomAttributes(typeof(FromCapAttribute)).Any()
                }).ToList();

                var capName = parameters.FirstOrDefault(x => !x.IsFromCap)?.ParameterType.FullName;
                if (string.IsNullOrEmpty(capName)) continue;

                yield return new ConsumerExecutorDescriptor
                {
                    Attribute = new CapSubscribeAttribute(capName)
                    {
                        Group = topic
                    },
                    Parameters = parameters,
                    MethodInfo = method,
                    ImplTypeInfo = typeInfo,
                    TopicNamePrefix = _capOptions.TopicNamePrefix,
                    ServiceTypeInfo = typeInfo
                };
            }
        }
    }
}