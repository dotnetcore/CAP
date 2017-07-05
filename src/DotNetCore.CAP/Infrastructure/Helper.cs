using System;
using System.Reflection;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Infrastructure
{
    internal static class Helper
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static JsonSerializerSettings SerializerSettings;

        public static void SetSerializerSettings(JsonSerializerSettings setting)
        {
            SerializerSettings = setting;
        }

        public static string ToJson(object value)
        {
            return value != null
                ? JsonConvert.SerializeObject(value, SerializerSettings)
                : null;
        }

        public static T FromJson<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, SerializerSettings)
                : default(T);
        }

        public static object FromJson(string value, Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, SerializerSettings)
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

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (!typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}