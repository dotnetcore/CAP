// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP.Filter
{
    public class FilterContext : ConsumerContext
    {
        public FilterContext(ConsumerContext context) : base(context)
        {

        }
    }
}
