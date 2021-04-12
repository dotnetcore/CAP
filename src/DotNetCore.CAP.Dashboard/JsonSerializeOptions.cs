using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCore.CAP.Dashboard
{
    public static class JsonSerializeOptions
    {
        public static JsonSerializerOptions Default = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }
}