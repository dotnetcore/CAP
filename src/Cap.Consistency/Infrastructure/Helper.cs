using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Cap.Consistency.Infrastructure
{
    internal static class Helper
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static JsonSerializerSettings SerializerSettings;

        public static void SetSerializerSettings(JsonSerializerSettings setting) {
            SerializerSettings = setting;
        }

        public static string ToJson(object value) {
            return value != null
                ? JsonConvert.SerializeObject(value, SerializerSettings)
                : null;
        }

        public static T FromJson<T>(string value) {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, SerializerSettings)
                : default(T);
        }

        public static object FromJson(string value, Type type) {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, SerializerSettings)
                : null;
        }

        public static long ToTimestamp(DateTime value) {
            var elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime FromTimestamp(long value) {
            return Epoch.AddSeconds(value);
        }
    }
}
