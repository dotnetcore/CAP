// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// A descriptor of user definition method.
    /// </summary>
    public class ConsumerExecutorDescriptor
    {
        public TypeInfo? ServiceTypeInfo { get; set; }

        public MethodInfo MethodInfo { get; set; } = default!;

        public TypeInfo ImplTypeInfo { get; set; } = default!;

        public TopicAttribute Attribute { get; set; } = default!;

        public TopicAttribute? ClassAttribute { get; set; }

        public IList<ParameterDescriptor> Parameters { get; set; } = new List<ParameterDescriptor>();

        public string? TopicNamePrefix { get; set; }

        private string? _topicName;
        /// <summary>
        /// Topic name based on both <see cref="Attribute"/> and <see cref="ClassAttribute"/>.
        /// </summary>
        public string TopicName
        {
            get
            {
                if (_topicName == null) 
                {
                    if (ClassAttribute != null && Attribute.IsPartial)
                    {
                        // Allows class level attribute name to end with a '.' and allows methods level attribute to start with a '.'.
                        _topicName = $"{ClassAttribute.Name.TrimEnd('.')}.{Attribute.Name.TrimStart('.')}";
                    }
                    else
                    {
                        _topicName = Attribute.Name;
                    }

                    if (!string.IsNullOrEmpty(TopicNamePrefix) && !string.IsNullOrEmpty(_topicName))
                    {
                        _topicName = $"{TopicNamePrefix}.{_topicName}";
                    }
                }
                return _topicName;
            }
        }
    }

    public class ConsumerExecutorDescriptorComparer : IEqualityComparer<ConsumerExecutorDescriptor>
    {
        private readonly ILogger _logger;

        public ConsumerExecutorDescriptorComparer(ILogger logger)
        {
            _logger = logger;
        }

        public bool Equals(ConsumerExecutorDescriptor? x, ConsumerExecutorDescriptor? y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y))
            {
                _logger.ConsumerDuplicates(x.TopicName,x.Attribute.Group);
                return true;
            }

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
            {
                return false;
            }

            //Check whether the ConsumerExecutorDescriptor' properties are equal.
            var ret = x.TopicName.Equals(y.TopicName, StringComparison.OrdinalIgnoreCase) &&
                x.Attribute.Group.Equals(y.Attribute.Group, StringComparison.OrdinalIgnoreCase);

            if (ret)
            {
                _logger.ConsumerDuplicates(x.TopicName, x.Attribute.Group);
            }

            return ret;
        }

        public int GetHashCode(ConsumerExecutorDescriptor? obj)
        {
            //Check whether the object is null
            if (obj is null) return 0;

            //Get hash code for the Attribute Group field if it is not null.
            int hashAttributeGroup = obj.Attribute?.Group == null ? 0 : obj.Attribute.Group.GetHashCode();

            //Get hash code for the TopicName field.
            int hashTopicName = obj.TopicName.GetHashCode();

            //Calculate the hash code.
            return hashAttributeGroup ^ hashTopicName;
        }
    }

    public class ParameterDescriptor
    {
        public string Name { get; set; } = default!;

        public Type ParameterType { get; set; } = default!;

        public bool IsFromCap { get; set; }
    }
}