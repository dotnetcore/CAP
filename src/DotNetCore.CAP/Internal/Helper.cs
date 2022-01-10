// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DotNetCore.CAP.Internal
{
    public static class Helper
    {
        public static bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            if (!typeInfo.IsPublic)
            {
                return false;
            }

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            return !typeInfo.ContainsGenericParameters
                   && typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsComplexType(Type type)
        {
            return !CanConvertFromString(type);
        }

        public static string WildcardToRegex(string wildcard)
        {
            if (wildcard.IndexOf('*') >= 0)
            {
                return ("^" + wildcard + "$").Replace("*", "[0-9a-zA-Z]+").Replace(".", "\\.");
            }

            if (wildcard.IndexOf('#') >= 0)
            {
                return ("^" + wildcard.Replace(".", "\\.") + "$").Replace("#", "[0-9a-zA-Z\\.]+");
            }

            return wildcard;
        }

        public static string Normalized(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            var pattern = "[\\>\\.\\ \\*]";
            return Regex.IsMatch(name, pattern) ? Regex.Replace(name, pattern, "_") : name;
        }

        public static bool IsUsingType<T>(in Type type)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return type.GetFields(flags).Any(x => x.FieldType == typeof(T));
        }

        public static bool IsInnerIP(string ipAddress)
        {
            var ipNum = GetIpNum(ipAddress);

            //Private IP：
            //category A: 10.0.0.0-10.255.255.255
            //category B: 172.16.0.0-172.31.255.255
            //category C: 192.168.0.0-192.168.255.255  

            var aBegin = GetIpNum("10.0.0.0");
            var aEnd = GetIpNum("10.255.255.255");
            var bBegin = GetIpNum("172.16.0.0");
            var bEnd = GetIpNum("172.31.255.255");
            var cBegin = GetIpNum("192.168.0.0");
            var cEnd = GetIpNum("192.168.255.255");
            return IsInner(ipNum, aBegin, aEnd) || IsInner(ipNum, bBegin, bEnd) || IsInner(ipNum, cBegin, cEnd);
        }

        private static long GetIpNum(string ipAddress)
        {
            var ip = ipAddress.Split('.');
            long a = int.Parse(ip[0]);
            long b = int.Parse(ip[1]);
            long c = int.Parse(ip[2]);
            long d = int.Parse(ip[3]);

            var ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            return ipNum;
        }

        private static bool IsInner(long userIp, long begin, long end)
        {
            return userIp >= begin && userIp <= end;
        }

        private static bool CanConvertFromString(Type destinationType)
        {
            destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            return IsSimpleType(destinationType) ||
                   TypeDescriptor.GetConverter(destinationType).CanConvertFrom(typeof(string));
        }

        private static bool IsSimpleType(Type type)
        {
            return type.GetTypeInfo().IsPrimitive ||
                   type == typeof(decimal) ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(Guid) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Uri);
        }
    }
}