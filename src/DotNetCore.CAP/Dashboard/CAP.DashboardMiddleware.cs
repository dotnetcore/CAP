// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class DashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DashboardOptions _options;
        private readonly RouteCollection _routes;
        private readonly IStorage _storage;

        public DashboardMiddleware(RequestDelegate next, DashboardOptions options, IStorage storage,
            RouteCollection routes)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        }

        public Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(_options.PathMatch,
                out var matchedPath, out var remainingPath))
            {
                return _next(context);
            }

            // Update the path
            var path = context.Request.Path;
            var pathBase = context.Request.PathBase;
            context.Request.PathBase = pathBase.Add(matchedPath);
            context.Request.Path = remainingPath;

            try
            {
                var dashboardContext = new CapDashboardContext(_storage, _options, context);
                var findResult = _routes.FindDispatcher(context.Request.Path.Value);

                if (findResult == null)
                {
                    return _next.Invoke(context);
                }

                if (_options.Authorization.Any(filter => !filter.Authorize(dashboardContext)))
                {
                    var isAuthenticated = context.User?.Identity?.IsAuthenticated;

                    context.Response.StatusCode = isAuthenticated == true
                        ? (int) HttpStatusCode.Forbidden
                        : (int) HttpStatusCode.Unauthorized;

                    return Task.CompletedTask;
                }

                dashboardContext.UriMatch = findResult.Item2;

                return findResult.Item1.Dispatch(dashboardContext);
            }
            finally
            {
                context.Request.PathBase = pathBase;
                context.Request.Path = path;
            }
        }
    }
}