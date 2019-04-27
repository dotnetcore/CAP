// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A descriptor of user definition method.
    /// </summary>
    public class ConsumerExecutorDescriptor
    {
        public MethodInfo MethodInfo { get; set; }

        public TypeInfo ImplTypeInfo { get; set; }

        public TopicAttribute Attribute { get; set; }
    }
}