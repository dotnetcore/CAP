// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// A default <see cref="T:DotNetCore.CAP.Abstractions.IConsumerServiceSelector" /> implementation.
    /// </summary>
    internal class DefaultConsumerServiceSelector : IConsumerServiceSelector
    {
        private readonly CapOptions _capOptions;
        private readonly IServiceProvider _serviceProvider;
        private List<RegexExecuteDescriptor<ConsumerExecutorDescriptor>> _asteriskList;
        private List<RegexExecuteDescriptor<ConsumerExecutorDescriptor>> _poundList;

        /// <summary>
        /// Creates a new <see cref="DefaultConsumerServiceSelector" />.
        /// </summary>
        public DefaultConsumerServiceSelector(IServiceProvider serviceProvider, CapOptions capOptions)
        {
            _serviceProvider = serviceProvider;
            _capOptions = capOptions;
        }

        public IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates()
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            executorDescriptorList.AddRange(FindConsumersFromInterfaceTypes(_serviceProvider));

            executorDescriptorList.AddRange(FindConsumersFromControllerTypes());

            return executorDescriptorList;
        }

        public ConsumerExecutorDescriptor SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            var result = MatchUsingName(key, executeDescriptor);
            if (result != null)
            {
                return result;
            }

            //[*] match with regex, i.e.  foo.*.abc
            result = MatchAsteriskUsingRegex(key, executeDescriptor);
            if (result != null)
            {
                return result;
            }

            //[#] match regex, i.e. foo.#
            result = MatchPoundUsingRegex(key, executeDescriptor);
            return result;
        }

        private IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(
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
                    {
                        continue;
                    }

                    executorDescriptorList.AddRange(GetTopicAttributesDescription(typeInfo));
                }

                return executorDescriptorList;
            }
        }

        private IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromControllerTypes()
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            var types = Assembly.GetEntryAssembly().ExportedTypes;
            foreach (var type in types)
            {
                var typeInfo = type.GetTypeInfo();
                if (Helper.IsController(typeInfo))
                {
                    executorDescriptorList.AddRange(GetTopicAttributesDescription(typeInfo));
                }
            }

            return executorDescriptorList;
        }

        private IEnumerable<ConsumerExecutorDescriptor> GetTopicAttributesDescription(TypeInfo typeInfo)
        {
            foreach (var method in typeInfo.DeclaredMethods)
            {
                var topicAttr = method.GetCustomAttributes<TopicAttribute>(true);
                var topicAttributes = topicAttr as IList<TopicAttribute> ?? topicAttr.ToList();

                if (!topicAttributes.Any())
                {
                    continue;
                }

                foreach (var attr in topicAttributes)
                {
                    if (attr.Group == null)
                    {
                        attr.Group = _capOptions.DefaultGroup;
                    }

                    yield return InitDescriptor(attr, method, typeInfo);
                }
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

        private ConsumerExecutorDescriptor MatchUsingName(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            return executeDescriptor.FirstOrDefault(x => x.Attribute.Name == key);
        }

        private ConsumerExecutorDescriptor MatchAsteriskUsingRegex(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            if (_asteriskList == null)
            {
                _asteriskList = executeDescriptor
                    .Where(x => x.Attribute.Name.IndexOf('*') >= 0)
                    .Select(x => new RegexExecuteDescriptor<ConsumerExecutorDescriptor>
                {
                    Name = ("^" + x.Attribute.Name + "$").Replace("*", "[a-zA-Z]+").Replace(".", "\\."),
                    Descriptor = x
                }).ToList();
            }
            foreach (var red in _asteriskList)
            {
                if (Regex.IsMatch(key, red.Name, RegexOptions.Singleline))
                {
                    return red.Descriptor;
                }
            }

            return null;
        }

        private ConsumerExecutorDescriptor MatchPoundUsingRegex(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
        {
            if (_poundList == null)
            {
                _poundList = executeDescriptor
                    .Where(x => x.Attribute.Name.IndexOf('#') >= 0)
                    .Select(x => new RegexExecuteDescriptor<ConsumerExecutorDescriptor>
                {
                    Name = ("^" + x.Attribute.Name + "$").Replace("#", "[a-zA-Z\\.]+"),
                    Descriptor = x
                }).ToList();
            }

            foreach (var red in _poundList)
            {
                if (Regex.IsMatch(key, red.Name, RegexOptions.Singleline))
                {
                    return red.Descriptor;
                }
            }

            return null;
        }


        private class RegexExecuteDescriptor<T>
        {
            public string Name { get; set; }

            public T Descriptor { get; set; }
        }
    }
}