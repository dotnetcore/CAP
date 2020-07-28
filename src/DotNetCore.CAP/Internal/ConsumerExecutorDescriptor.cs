// Copyright (c) .NET Core Community. All rights reserved.
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

        /// <summary>
        /// Topic name based on both <see cref="Attribute"/> and <see cref="ClassAttribute"/>.
        /// </summary>
        public string TopicName
        {
            get
            {
                if (ClassAttribute != null && Attribute.IsPartial)
                {
                    // Allows class level attribute name to end with a '.' and allows methods level attribute to start with a '.'.
                    return string.Join(".", $"{ClassAttribute.Name}.{Attribute.Name}".Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    return Attribute.Name;
                }
            }
        }
    }

    public class ParameterDescriptor
    {
        public string Name { get; set; }

        public Type ParameterType { get; set; }

        public bool IsFromCap { get; set; }
    }
}