// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Abstractions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// An attribute for subscribe Kafka messages.
    /// </summary>
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name)
            : base(name)
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }
}