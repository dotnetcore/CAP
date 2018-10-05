// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using DotNetCore.CAP.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotNetCore.CAP.Infrastructure
{
    public static class Helper
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
        private static JsonSerializerSettings _serializerSettings;

        public static void SetSerializerSettings(JsonSerializerSettings setting)
        {
            _serializerSettings = setting;
        }

        public static string ToJson(object value)
        {
            return value != null
                ? JsonConvert.SerializeObject(value, _serializerSettings)
                : null;
        }

        public static T FromJson<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, _serializerSettings)
                : default(T);
        }

        public static object FromJson(string value, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return value != null
                ? JsonConvert.DeserializeObject(value, type, _serializerSettings)
                : null;
        }

        public static long ToTimestamp(DateTime value)
        {
            var elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }


        public static DateTime FromTimestamp(long value)
        {
            return Epoch.AddSeconds(value);
        }

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

            return !typeInfo.ContainsGenericParameters
                   && typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsComplexType(Type type)
        {
            return !CanConvertFromString(type);
        }

        public static string AddExceptionProperty(string json, Exception exception)
        {
            var jObject = ToJObject(exception);
            return AddJsonProperty(json, "ExceptionMessage", jObject);
        }

        public static string AddTracingHeaderProperty(string json, TracingHeaders headers)
        {
            var jObject = ToJObject(headers);
            return AddJsonProperty(json, nameof(TracingHeaders), jObject);
        }

        public static bool TryExtractTracingHeaders(string json, out TracingHeaders headers, out string removedHeadersJson)
        {
            var jObj = JObject.Parse(json);
            var jToken = jObj[nameof(TracingHeaders)];
            if (jToken != null)
            {
                headers = new TracingHeaders();
                foreach (var item in jToken.ToObject<Dictionary<string,string>>())
                {
                    headers.Add(item.Key, item.Value);
                }

                jObj.Remove(nameof(TracingHeaders)); 
                removedHeadersJson = jObj.ToString();
                return true;
            }

            headers = null;
            removedHeadersJson = null;
            return false;
        }

        public static bool IsInnerIP(string ipAddress)
        {
            bool isInnerIp;
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
            isInnerIp = IsInner(ipNum, aBegin, aEnd) || IsInner(ipNum, bBegin, bEnd) || IsInner(ipNum, cBegin, cEnd);
            return isInnerIp;
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

        private static JObject ToJObject(Exception exception)
        {
            return JObject.FromObject(new
            {
                exception.Source,
                exception.Message,
                InnerMessage = exception.InnerException?.Message
            });
        }

        private static JObject ToJObject(TracingHeaders headers)
        {
            var jobj = new JObject();
            foreach (var keyValuePair in headers)
            {
                jobj[keyValuePair.Key] = keyValuePair.Value;
            }
            return jobj;
        }

        private static string AddJsonProperty(string json, string propertyName, JObject propertyValue)
        {
            var jObj = JObject.Parse(json);

            if (jObj.TryGetValue(propertyName, out var _))
            {
                jObj[propertyName] = propertyValue;
            }
            else
            {
                jObj.Add(new JProperty(propertyName, propertyValue));
            }

            return jObj.ToString(Formatting.None);
        }
    }
}