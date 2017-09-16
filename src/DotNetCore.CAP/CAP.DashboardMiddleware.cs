using System;
using System.Net;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.AspNetCore.Http;

namespace DotNetCore.CAP
{
    public class DashboardMiddleware
    {
        private readonly DashboardOptions _options;
        private readonly RequestDelegate _next;
        private readonly IStorage _storage;
        private readonly RouteCollection _routes;

        public DashboardMiddleware(RequestDelegate next, DashboardOptions options, IStorage storage, RouteCollection routes)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        }

        public Task Invoke(HttpContext httpContext)
        {
            var context = new CapDashboardContext(_storage, _options, httpContext);
            var findResult = _routes.FindDispatcher(httpContext.Request.Path.Value);

            if (findResult == null)
            {
                return _next.Invoke(httpContext);
            }

            foreach (var filter in _options.Authorization)
            {
                if (!filter.Authorize(context))
                {
                    var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated;

                    httpContext.Response.StatusCode = isAuthenticated == true
                        ? (int)HttpStatusCode.Forbidden
                        : (int)HttpStatusCode.Unauthorized;

                    return Task.FromResult(0);
                }
            }

            context.UriMatch = findResult.Item2;

            return findResult.Item1.Dispatch(context);
        }
    }
}
