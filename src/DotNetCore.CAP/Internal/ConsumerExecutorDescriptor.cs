﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// A descriptor of user definition method.
    /// </summary>
    public class ConsumerExecutorDescriptor
    {
        public TypeInfo ServiceTypeInfo { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public TypeInfo ImplTypeInfo { get; set; }

        public TopicAttribute Attribute { get; set; }

        public TopicAttribute ClassAttribute { get; set; }

        public IList<ParameterDescriptor> Parameters { get; set; }

        private string _topicName;
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
                }
                return _topicName;
            }
        }
    }

    public class ConsumerExecutorDescriptorComparer : IEqualityComparer<ConsumerExecutorDescriptor>
    {
        public bool Equals(ConsumerExecutorDescriptor x, ConsumerExecutorDescriptor y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
            {
                return false;
            }

            //Check whether the ConsumerExecutorDescriptor' properties are equal.
            return x.TopicName.Equals(y.TopicName, StringComparison.OrdinalIgnoreCase) &&
                x.Attribute.Group.Equals(y.Attribute.Group, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ConsumerExecutorDescriptor obj)
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
        public string Name { get; set; }

        public Type ParameterType { get; set; }

        public bool IsFromCap { get; set; }
    }
}