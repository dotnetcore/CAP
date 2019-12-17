// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Internal;

namespace DotNetCore.CAP.Dashboard
{
    internal struct CoercedAwaitableInfo
    {
        public AwaitableInfo AwaitableInfo { get; }
        public Expression CoercerExpression { get; }
        public Type CoercerResultType { get; }
        public bool RequiresCoercion => CoercerExpression != null;

        public CoercedAwaitableInfo(AwaitableInfo awaitableInfo)
        {
            AwaitableInfo = awaitableInfo;
            CoercerExpression = null;
            CoercerResultType = null;
        }

        public CoercedAwaitableInfo(Expression coercerExpression, Type coercerResultType,
            AwaitableInfo coercedAwaitableInfo)
        {
            CoercerExpression = coercerExpression;
            CoercerResultType = coercerResultType;
            AwaitableInfo = coercedAwaitableInfo;
        }

        public static bool IsTypeAwaitable(Type type, out CoercedAwaitableInfo info)
        {
            if (AwaitableInfo.IsTypeAwaitable(type, out var directlyAwaitableInfo))
            {
                info = new CoercedAwaitableInfo(directlyAwaitableInfo);
                return true;
            }

            info = default(CoercedAwaitableInfo);
            return false;
        }
    }
}