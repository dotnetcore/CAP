// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Internal;

namespace DotNetCore.CAP.Dashboard
{
    public class HtmlHelper
    {
        public static string MethodEscaped(MethodInfo method)
        {
            var @public = WrapKeyword("public");
            var async = string.Empty;
            string @return;

            var isAwaitable = CoercedAwaitableInfo.IsTypeAwaitable(method.ReturnType, out var coercedAwaitableInfo);
            if (isAwaitable)
            {
                async = WrapKeyword("async");
                var asyncResultType = coercedAwaitableInfo.AwaitableInfo.ResultType;

                @return = WrapType("Task") + WrapIdentifier("<") + WrapType(asyncResultType) + WrapIdentifier(">");
            }
            else
            {
                @return = WrapType(method.ReturnType);
            }

            var name = method.Name;

            string paramType = null;
            string paramName = null;

            var @params = method.GetParameters();
            if (@params.Length == 1)
            {
                var firstParam = @params[0];
                var firstParamType = firstParam.ParameterType;
                paramType = WrapType(firstParamType);
                paramName = firstParam.Name;
            }

            var paramString = paramType == null ? "();" : $"({paramType} {paramName});";

            var outputString = @public + " " + (string.IsNullOrEmpty(async) ? "" : async + " ") + @return + " " + name +
                               paramString;

            return outputString;
        }

        private static string WrapType(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (type.Name == "Void")
            {
                return WrapKeyword(type.Name.ToLower());
            }

            if (Helper.IsComplexType(type))
            {
                return WrapType(type.Name);
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                return WrapKeyword(type.Name.ToLower());
            }

            return WrapType(type.Name);
        }

        private static string WrapIdentifier(string value)
        {
            return value;
        }

        private static string WrapKeyword(string value)
        {
            return Span("keyword", value);
        }

        private static string WrapType(string value)
        {
            return Span("type", value);
        }

        private static string Span(string @class, string value)
        {
            return $"<span class=\"{@class}\">{value}</span>";
        }
    }
}