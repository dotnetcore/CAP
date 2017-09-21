using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; 

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester.Middleware
{
    public class HttpRequesterMiddleware : GatewayProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly ILogger _logger;

        public HttpRequesterMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IHttpRequester requester, 
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requester = requester;
            _logger = loggerFactory.CreateLogger<HttpRequesterMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling requester middleware");

            var response = await _requester.GetResponse(Request);
 

            _logger.LogDebug("setting http response message");

            SetHttpResponseMessageThisRequest(response);

            _logger.LogDebug("returning to calling middleware");
        }
    }
}