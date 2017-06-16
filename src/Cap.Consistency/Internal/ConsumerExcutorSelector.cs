using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Consumer;
using Cap.Consistency.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cap.Consistency.Internal
{
    public class ConsumerExcutorSelector : IConsumerExcutorSelector
    {
        private readonly IServiceProvider _serviceProvider;

        public ConsumerExcutorSelector(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public ConsumerExecutorDescriptor SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor) {
            return executeDescriptor.FirstOrDefault(x => x.Attribute.Name == key);
        }

        public IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(TopicContext context) {

            var consumerServices = context.ServiceProvider.GetServices<IConsumerService>();

            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();
            foreach (var service in consumerServices) {
                var typeInfo = service.GetType().GetTypeInfo();
                if (!typeof(IConsumerService).GetTypeInfo().IsAssignableFrom(typeInfo)) {
                    continue;
                }

                foreach (var method in typeInfo.DeclaredMethods) {

                    var topicAttr = method.GetCustomAttribute<TopicAttribute>(true);
                    if (topicAttr == null) continue;

                    executorDescriptorList.Add(InitDescriptor(topicAttr, method, typeInfo));
                }
            }

            return executorDescriptorList;
        }
        private ConsumerExecutorDescriptor InitDescriptor(TopicAttribute attr,
            MethodInfo methodInfo, TypeInfo implType
            ) {
            var descriptor = new ConsumerExecutorDescriptor();

            descriptor.Attribute = attr;
            descriptor.MethodInfo = methodInfo;
            descriptor.ImplTypeInfo = implType;

            return descriptor;
        }
    }
}
