// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Internal;

/// <inheritdoc />
/// <summary>
/// A default <see cref="T:DotNetCore.CAP.Abstractions.IConsumerServiceSelector" /> implementation.
/// </summary>
public class ConsumerServiceSelector : IConsumerServiceSelector
{
    /// <summary>
    /// since this class be designed as a Singleton service,the following two list must be thread safe!
    /// </summary>
    private readonly ConcurrentDictionary<string, List<RegexExecuteDescriptor<ConsumerExecutorDescriptor>>> _cacheList;

    private readonly CapOptions _capOptions;
    private readonly ILogger<ConsumerServiceSelector> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new <see cref="ConsumerServiceSelector" />.
    /// </summary>
    public ConsumerServiceSelector(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _capOptions = serviceProvider.GetRequiredService<IOptions<CapOptions>>().Value;
        _logger = serviceProvider.GetRequiredService<ILogger<ConsumerServiceSelector>>();
        _cacheList = new ConcurrentDictionary<string, List<RegexExecuteDescriptor<ConsumerExecutorDescriptor>>>();
    }

    public IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates()
    {
        var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

        executorDescriptorList.AddRange(FindConsumersFromInterfaceTypes(_serviceProvider));

        executorDescriptorList.AddRange(FindConsumersFromControllerTypes());

        executorDescriptorList =
            executorDescriptorList.Distinct(new ConsumerExecutorDescriptorComparer(_logger)).ToList();

        return executorDescriptorList;
    }

    public ConsumerExecutorDescriptor? SelectBestCandidate(string key,
        IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
    {
        if (executeDescriptor.Count == 0) return null;

        var result = MatchUsingName(key, executeDescriptor);
        if (result != null) return result;

        //[*] match with regex, i.e.  foo.*.abc
        //[#] match regex, i.e. foo.#
        return MatchWildcardUsingRegex(key, executeDescriptor);
    }

    protected virtual IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(
        IServiceProvider provider)
    {
        var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

        var capSubscribeTypeInfo = typeof(ICapSubscribe).GetTypeInfo();

        using var scope = provider.CreateScope();
        var scopeProvider = scope.ServiceProvider;

        var serviceCollection = scopeProvider.GetRequiredService<IServiceCollection>();

        foreach (var service in serviceCollection
                     .Where(o => o.ImplementationType != null || o.ImplementationFactory != null))
        {
            var detectType = service.ImplementationType ?? service.ServiceType;
            if (!capSubscribeTypeInfo.IsAssignableFrom(detectType)) continue;

            var actualType = service.ImplementationType;
            if (actualType == null && service.ImplementationFactory != null)
                actualType = scopeProvider.GetRequiredService(service.ServiceType).GetType();

            if (actualType == null) throw new NullReferenceException(nameof(service.ServiceType));

            executorDescriptorList.AddRange(GetTopicAttributesDescription(actualType.GetTypeInfo(),
                service.ServiceType.GetTypeInfo()));
        }

        return executorDescriptorList;
    }

    protected virtual IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromControllerTypes()
    {
        var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

        var types = Assembly.GetEntryAssembly()!.ExportedTypes;
        foreach (var type in types)
        {
            var typeInfo = type.GetTypeInfo();
            if (Helper.IsController(typeInfo)) executorDescriptorList.AddRange(GetTopicAttributesDescription(typeInfo));
        }

        return executorDescriptorList;
    }

    protected IEnumerable<ConsumerExecutorDescriptor> GetTopicAttributesDescription(TypeInfo typeInfo,
        TypeInfo? serviceTypeInfo = null)
    {
        var topicClassAttribute = typeInfo.GetCustomAttribute<TopicAttribute>(true);

        foreach (var method in typeInfo.GetRuntimeMethods())
        {
            var topicMethodAttributes = method.GetCustomAttributes<TopicAttribute>(true);

            // Ignore partial attributes when no topic attribute is defined on class.
            if (topicClassAttribute is null)
                topicMethodAttributes = topicMethodAttributes.Where(x => !x.IsPartial && x.Name != null);

            if (!topicMethodAttributes.Any()) continue;

            foreach (var attr in topicMethodAttributes)
            {
                SetSubscribeAttribute(attr);

                var parameters = method.GetParameters()
                    .Select(parameter => new ParameterDescriptor
                    {
                        Name = parameter.Name!,
                        ParameterType = parameter.ParameterType,
                        IsFromCap = parameter.GetCustomAttributes(typeof(FromCapAttribute)).Any()
                                    || typeof(CancellationToken).IsAssignableFrom(parameter.ParameterType)
                    }).ToList();

                yield return InitDescriptor(attr, method, typeInfo, serviceTypeInfo, parameters, topicClassAttribute);
            }
        }
    }

    protected virtual void SetSubscribeAttribute(TopicAttribute attribute)
    {
        var prefix = !string.IsNullOrEmpty(_capOptions.GroupNamePrefix)
            ? $"{_capOptions.GroupNamePrefix}."
            : string.Empty;
        attribute.Group = $"{prefix}{attribute.Group ?? _capOptions.DefaultGroupName}.{_capOptions.Version}";
    }

    private ConsumerExecutorDescriptor InitDescriptor(
        TopicAttribute attr,
        MethodInfo methodInfo,
        TypeInfo implType,
        TypeInfo? serviceTypeInfo,
        IList<ParameterDescriptor> parameters,
        TopicAttribute? classAttr = null)
    {
        var descriptor = new ConsumerExecutorDescriptor
        {
            Attribute = attr,
            ClassAttribute = classAttr,
            MethodInfo = methodInfo,
            ImplTypeInfo = implType,
            ServiceTypeInfo = serviceTypeInfo,
            Parameters = parameters,
            TopicNamePrefix = _capOptions.TopicNamePrefix
        };

        return descriptor;
    }

    private ConsumerExecutorDescriptor? MatchUsingName(string key,
        IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        return executeDescriptor.FirstOrDefault(x =>
            x.TopicName.Equals(key, StringComparison.InvariantCultureIgnoreCase));
    }

    private ConsumerExecutorDescriptor? MatchWildcardUsingRegex(string key,
        IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor)
    {
        var group = executeDescriptor.First().Attribute.Group;
        if (!_cacheList.TryGetValue(group, out var tmpList))
        {
            tmpList = executeDescriptor.Select(x => new RegexExecuteDescriptor<ConsumerExecutorDescriptor>
            {
                Name = Helper.WildcardToRegex(x.TopicName),
                Descriptor = x
            }).ToList();
            _cacheList.TryAdd(group, tmpList);
        }

        foreach (var red in tmpList)
            if (Regex.IsMatch(key, red.Name, RegexOptions.Singleline))
                return red.Descriptor;

        return null;
    }

    private class RegexExecuteDescriptor<T>
    {
        public string Name { get; set; } = default!;

        public T Descriptor { get; set; } = default!;
    }
}