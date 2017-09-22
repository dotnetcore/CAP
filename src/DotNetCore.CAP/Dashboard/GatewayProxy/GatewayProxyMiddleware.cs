using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard.GatewayProxy.Requester;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class GatewayProxyMiddleware : GatewayProxyMiddlewareBase
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IRequestMapper _requestMapper;
        private readonly IHttpRequester _requester;

        public GatewayProxyMiddleware(RequestDelegate next,
           ILoggerFactory loggerFactory,
           IRequestMapper requestMapper,
           IHttpRequester requester)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<GatewayProxyMiddleware>();
            _requestMapper = requestMapper;
            _requester = requester;
        }

        public async Task Invoke(HttpContext context, IRequestScopedDataRepository requestScopedDataRepository)
        {
            _requestScopedDataRepository = requestScopedDataRepository;

            _logger.LogDebug("started calling gateway proxy middleware");

            var downstreamRequest = await _requestMapper.Map(context.Request);

            _logger.LogDebug("setting downstream request");

            SetDownstreamRequest(downstreamRequest);

            _logger.LogDebug("setting upstream request");

            SetUpstreamRequestForThisRequest(DownstreamRequest);

            var uriBuilder = new UriBuilder(DownstreamRequest.RequestUri)
            {
                //Path = dsPath.Data.Value,
                //Scheme = DownstreamRoute.ReRoute.DownstreamScheme
            };

            DownstreamRequest.RequestUri = uriBuilder.Uri;

            _logger.LogDebug("started calling request");

            var response = await _requester.GetResponse(Request);

            _logger.LogDebug("setting http response message");

            SetHttpResponseMessageThisRequest(response);

            _logger.LogDebug("returning to calling middleware");
        }
    }
}