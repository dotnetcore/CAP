using Castle.Core;
using Castle.DynamicProxy;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetCore.CAP.CastleDynamicProxyTest
{
    public class CastleCoreConsumerServiceSelector : DefaultConsumerServiceSelector
    {
        public CastleCoreConsumerServiceSelector(IServiceProvider serviceProvider, CapOptions capOptions) 
            : base(serviceProvider, capOptions)
        {

        }

        protected override IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            using (var scoped = provider.CreateScope())
            {
                var scopedProvider = scoped.ServiceProvider;
                var consumerServices = scopedProvider.GetServices<ICapSubscribe>();
                foreach (var service in consumerServices)
                {
                    var serviceType = service.GetType();
                    // Castle dynamic proxy...
                    TypeInfo typeInfo = ProxyServices.IsDynamicProxy(serviceType) ? ProxyUtil.GetUnproxiedType(service).GetTypeInfo()
                        : serviceType.GetTypeInfo();

                    if (!typeof(ICapSubscribe).GetTypeInfo().IsAssignableFrom(typeInfo))
                    {
                        continue;
                    }

                    executorDescriptorList.AddRange(GetTopicAttributesDescription(typeInfo));
                }

                return executorDescriptorList;
            }
        }
    }
}
