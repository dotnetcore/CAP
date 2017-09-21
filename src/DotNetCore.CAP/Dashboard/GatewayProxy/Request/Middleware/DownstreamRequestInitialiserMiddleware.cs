namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    public class DownstreamRequestInitialiserMiddleware : GatewayProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IRequestMapper _requestMapper;

        public DownstreamRequestInitialiserMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            IRequestMapper requestMapper)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>();
            _requestMapper = requestMapper;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling request builder middleware");

            var downstreamRequest = await _requestMapper.Map(context.Request);
            
            SetDownstreamRequest(downstreamRequest);

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}