using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Test
{
    /// <summary>
    /// Allows caller to supply subscribe interface and attribute when adding services.
    /// </summary>
    /// <typeparam name="TSubscriber"></typeparam>
    /// <typeparam name="TSubscriptionAttribute"></typeparam>
    public class GenericConsumerServiceSelector<TSubscriber, TSubscriptionAttribute> : ConsumerServiceSelector
        where TSubscriptionAttribute : Attribute, INamedGroup
    {
        private readonly CapOptions _capOptions;
        
        public GenericConsumerServiceSelector(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _capOptions = serviceProvider.GetRequiredService<IOptions<CapOptions>>().Value;
        }

        /// <inheritdoc cref="ConsumerServiceSelector"/>
        protected override IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            using (var scoped = provider.CreateScope())
            {
                var scopedProvider = scoped.ServiceProvider;
                var subscribers = scopedProvider.GetServices<TSubscriber>();
                var subscriberTypeInfo = typeof(TSubscriber).GetTypeInfo();
                foreach (var service in subscribers)
                {
                    var serviceTypeInfo = service?.GetType().GetTypeInfo();
                    if (serviceTypeInfo == null || !subscriberTypeInfo.IsAssignableFrom(serviceTypeInfo))
                    {
                        continue;
                    }

                    var descriptors = _GetDescriptors(serviceTypeInfo);
                    executorDescriptorList.AddRange(descriptors);
                }

                return executorDescriptorList;
            }
        }

        private IEnumerable<ConsumerExecutorDescriptor> _GetDescriptors(TypeInfo typeInfo)
        {
            foreach (var method in typeInfo.DeclaredMethods)
            {
                var topicAttr = method.GetCustomAttributes<TSubscriptionAttribute>(true);
                var topicAttributes = topicAttr as IList<TSubscriptionAttribute> ?? topicAttr.ToList();

                if (!topicAttributes.Any())
                {
                    continue;
                }

                foreach (var attr in topicAttributes)
                {
                    _SetAttributeGroup(attr);

                    yield return new ConsumerExecutorDescriptor
                    {
                        Attribute = new CapSubscribeAttribute(attr.Name)
                        {
                            Group = attr.Group
                        },
                        MethodInfo = method,
                        ImplTypeInfo = typeInfo,
                        TopicNamePrefix = _capOptions.TopicNamePrefix,
                        Parameters = _GetParameterDescriptors(method)
                    };
                }
            }
        }

        private void _SetAttributeGroup(TSubscriptionAttribute attr)
        {
            if (attr.Group == null)
            {
                attr.Group = _capOptions.DefaultGroupName + "." + _capOptions.Version;
            }
            else
            {
                attr.Group = attr.Group + "." + _capOptions.Version;
            }

            if (!string.IsNullOrEmpty(_capOptions.GroupNamePrefix))
            {
                attr.Group = $"{_capOptions.GroupNamePrefix}.{attr.Group}";
            }
        }

        private IList<ParameterDescriptor> _GetParameterDescriptors(MethodInfo method)
        {
            var descriptors = method.GetParameters().Select(p => new ParameterDescriptor()
                {Name = p.Name, ParameterType = p.ParameterType, IsFromCap = p.GetCustomAttributes<FromCapAttribute>().Any()});
            return new List<ParameterDescriptor>(descriptors.ToArray());
        }
    }
    
    /// <summary>
    /// Implementers have a name and a group.
    /// </summary>
    public interface INamedGroup
    {
        string Name { get; }

        string Group { get; set; }
    }
}