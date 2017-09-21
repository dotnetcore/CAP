using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class HttpRequestBuilderMiddleware : GatewayProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public HttpRequestBuilderMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository )
            :base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<HttpRequestBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling request builder middleware");
 
            _logger.LogDebug("setting upstream request");

            SetUpstreamRequestForThisRequest(DownstreamRequest);

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}