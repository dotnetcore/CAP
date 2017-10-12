using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
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
            if (type == null) throw new ArgumentNullException(nameof(type));

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
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            return Epoch.AddSeconds(value);
        }

        public static bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
                return false;

            if (typeInfo.IsAbstract)
                return false;

            if (!typeInfo.IsPublic)
                return false;

            return !typeInfo.ContainsGenericParameters
                   && typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsComplexType(Type type)
        {
            return !CanConvertFromString(type);
        }

        public static JObject ToJObject(Exception exception)
        {
            return new JObject(new
            {
                Source = exception.Source,
                Message = exception.Message,
                InnerMessage = exception.InnerException?.Message
            });
        }

        public static string AddJsonProperty(string json, string propertyName, JObject propertyValue)
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

            return jObj.ToString();
        }

        public static string AddExceptionProperty(string json, Exception exception)
        {
            var jObject = ToJObject(exception);
            return AddJsonProperty(json, "ExceptionMessage", jObject);
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