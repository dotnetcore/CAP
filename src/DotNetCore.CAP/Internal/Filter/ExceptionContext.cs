// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP.Filter
{
    public class ExceptionContext : FilterContext
    {
        public ExceptionContext(ConsumerContext context, Exception e)
            : base(context)
        {
            Exception = e;
        }

        public Exception Exception { get; set; }

        public bool ExceptionHandled { get; set; }

        public object? Result { get; set; }
    }
}