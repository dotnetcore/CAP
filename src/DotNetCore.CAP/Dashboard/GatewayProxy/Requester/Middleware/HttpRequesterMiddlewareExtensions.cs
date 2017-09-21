using Microsoft.AspNetCore.Builder;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester.Middleware
{
    public static class HttpRequesterMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequesterMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequesterMiddleware>();
        }
    }
}