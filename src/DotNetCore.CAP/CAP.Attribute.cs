// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetCore.CAP.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name, bool isPartial = false)
            : base(name, isPartial)
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromCapAttribute : Attribute
    {
       
    }

    public class CapHeader : ReadOnlyDictionary<string, string?>
    {
        public CapHeader(IDictionary<string, string?> dictionary) : base(dictionary)
        {

        }
    }
}