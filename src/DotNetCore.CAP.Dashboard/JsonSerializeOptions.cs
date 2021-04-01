using System.Text.Json;

namespace DotNetCore.CAP.Dashboard
{
    public static class JsonSerializeOptions
    {
        public static JsonSerializerOptions Default = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}