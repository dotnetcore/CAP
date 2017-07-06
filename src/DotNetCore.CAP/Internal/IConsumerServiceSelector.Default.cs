﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
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
        /// Selects the best <see cref="ConsumerExecutorDescriptor"/> candidate from <paramref name="candidates"/> for the
        /// current message associated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="executeDescriptor"></param>
        /// <returns></returns>
        public ConsumerExecutorDescriptor SelectBestCandidate(string key,
            IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            return executeDescriptor.FirstOrDefault(x => x.Attribute.Name == key);
        }

        public IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            executorDescriptorList.AddRange(FindConsumersFromInterfaceTypes(provider));

            executorDescriptorList.AddRange(FindConsumersFromControllerTypes(provider));

            return executorDescriptorList;
        }


        private IReadOnlyList<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(
            IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            var consumerServices = provider.GetServices<ICapSubscribe>();
            foreach (var service in consumerServices)
            {
                var typeInfo = service.GetType().GetTypeInfo();
                if (!typeof(ICapSubscribe).GetTypeInfo().IsAssignableFrom(typeInfo))
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

        private IReadOnlyList<ConsumerExecutorDescriptor> FindConsumersFromControllerTypes(
            IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();
            // at cap startup time, find all Controller into the DI container,the type is object.
            var controllers = provider.GetServices<object>();
            foreach (var controller in controllers)
            {
                var typeInfo = controller.GetType().GetTypeInfo();
                //double check
                if (Helper.IsController(typeInfo))
                {
                    foreach (var method in typeInfo.DeclaredMethods)
                    {
                        var topicAttr = method.GetCustomAttribute<TopicAttribute>(true);
                        if (topicAttr == null) continue;

                        executorDescriptorList.Add(InitDescriptor(topicAttr, method, typeInfo));
                    }
                }
                continue;
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