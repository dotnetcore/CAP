// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetCore.CAP.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="T:DotNetCore.CAP.Abstractions.IConsumerServiceSelector" /> implementation that scanning subscribers from the assembly.
    /// </summary>
    public class AssemblyConsumerServiceSelector : ConsumerServiceSelector
    {
        private readonly Assembly[] _assemblies;

        public AssemblyConsumerServiceSelector(IServiceProvider serviceProvider, Assembly[] assemblies) : base(serviceProvider)
        {
            _assemblies = assemblies;
        }

        protected override IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(IServiceProvider provider)
        {
            var descriptors = new List<ConsumerExecutorDescriptor>();

            descriptors.AddRange(base.FindConsumersFromInterfaceTypes(provider));

            var assembliesToScan = _assemblies.Distinct().ToArray();

            var capSubscribeTypeInfo = typeof(ICapSubscribe).GetTypeInfo();

            foreach (var type in assembliesToScan.SelectMany(a => a.DefinedTypes))
            {
                if (!capSubscribeTypeInfo.IsAssignableFrom(type))
                {
                    continue;
                }

                descriptors.AddRange(GetTopicAttributesDescription(type));
            }

            return descriptors;
        }
    }
}
