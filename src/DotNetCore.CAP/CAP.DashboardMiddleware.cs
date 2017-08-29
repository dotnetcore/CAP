using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            _next = next;
            _options = options;
            _storage = storage;
            _routes = routes;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var context = new CapDashboardContext(_storage, _options, httpContext);
            var findResult = _routes.FindDispatcher(httpContext.Request.Path.Value);

            if (findResult == null)
            {
                return _next.Invoke(httpContext);
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
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
