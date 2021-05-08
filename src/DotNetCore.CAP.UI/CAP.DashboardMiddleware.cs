// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using DotNetCore.CAP.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public static class CapBuilderExtension
    {
        public static IApplicationBuilder UseCapDashboard(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            CheckRequirement(app);

            var provider = app.ApplicationServices;

            var option = provider.GetService<DashboardOptions>();
            if (option != null)
            {
                if (provider.GetService<DiscoveryOptions>() != null)
                {
                    app.UseMiddleware<GatewayProxyMiddleware>();
                }

                app.Map(option.PathMatch, false, x =>
                {
                    var builder = new RouteBuilder(x);

                    var methods = typeof(RouteActionProvider).GetMethods(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var method in methods)
                    {
                        var getAttr = method.GetCustomAttribute<HttpGetAttribute>();
                        if (getAttr != null)
                        {
                            builder.MapGet(getAttr.Template, (request, response, data) =>
                            {
                                var provider = new RouteActionProvider(request, response, data);
                                try
                                {
                                    method.Invoke(provider, null);
                                }
                                catch (Exception ex)
                                {
                                    response.StatusCode = StatusCodes.Status500InternalServerError;
                                    response.WriteAsync(ex.Message);
                                }

                                return Task.CompletedTask;
                            });
                        }

                        var postAttr = method.GetCustomAttribute<HttpPostAttribute>();
                        if (postAttr != null)
                        {
                            builder.MapPost(postAttr.Template, (request, response, data) =>
                            {
                                var provider = new RouteActionProvider(request, response, data);
                                try
                                {
                                    method.Invoke(provider, null);
                                }
                                catch (Exception ex)
                                {
                                    response.StatusCode = StatusCodes.Status500InternalServerError;
                                    response.WriteAsync(ex.Message);
                                }

                                return Task.CompletedTask;
                            });
                        }
                    }

                    var capRouter = builder.Build();

                    x.UseRouter(capRouter);

                });
            }

            return app;
        }

        private static void CheckRequirement(IApplicationBuilder app)
        {
            var marker = app.ApplicationServices.GetService<CapMarkerService>();
            if (marker == null)
            {
                throw new InvalidOperationException(
                    "AddCap() must be called on the service collection.   eg: services.AddCap(...)");
            }

            var messageQueueMarker = app.ApplicationServices.GetService<CapMessageQueueMakerService>();
            if (messageQueueMarker == null)
            {
                throw new InvalidOperationException(
                    "You must be config used message queue provider at AddCap() options!   eg: services.AddCap(options=>{ options.UseKafka(...) })");
            }

            var databaseMarker = app.ApplicationServices.GetService<CapStorageMarkerService>();
            if (databaseMarker == null)
            {
                throw new InvalidOperationException(
                    "You must be config used database provider at AddCap() options!   eg: services.AddCap(options=>{ options.UseSqlServer(...) })");
            }
        }
    }

    sealed class CapStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                app.UseCapDashboard();               
            };
        }
    }

    //public class DashboardMiddleware
    //{
    //    private readonly RequestDelegate _next;
    //    private readonly DashboardOptions _options;
    //    private readonly RouteCollection _routes;
    //    private readonly IDataStorage _storage;

    //    public DashboardMiddleware(RequestDelegate next, DashboardOptions options, IDataStorage storage,
    //        RouteCollection routes)
    //    {
    //        _next = next ?? throw new ArgumentNullException(nameof(next));
    //        _options = options ?? throw new ArgumentNullException(nameof(options));
    //        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    //        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
    //    }

    //    public async Task Invoke(HttpContext context)
    //    {
    //        if (!context.Request.Path.StartsWithSegments(_options.PathMatch,
    //            out var matchedPath, out var remainingPath))
    //        {
    //            await _next(context);
    //            return;
    //        }

    //        // Update the path
    //        var path = context.Request.Path;
    //        var pathBase = context.Request.PathBase;
    //        context.Request.PathBase = pathBase.Add(matchedPath);
    //        context.Request.Path = remainingPath;

    //        try
    //        {
    //            var dashboardContext = new CapDashboardContext(_storage, _options, context);
    //            var findResult = _routes.FindDispatcher(context.Request.Path.Value);

    //            if (findResult == null)
    //            {
    //                await _next.Invoke(context);
    //                return;
    //            }

    //            await _routes.RouteAsync(new RouteContext(context));

    //            foreach (var authorizationFilter in _options.Authorization)
    //            {
    //                var authenticateResult = await authorizationFilter.AuthorizeAsync(dashboardContext);
    //                if (authenticateResult) continue;

    //                var isAuthenticated = context.User?.Identity?.IsAuthenticated;

    //                if (_options.UseChallengeOnAuth)
    //                {
    //                    await context.ChallengeAsync(_options.DefaultChallengeScheme);
    //                    return;
    //                }

    //                context.Response.StatusCode = isAuthenticated == true
    //                    ? (int)HttpStatusCode.Forbidden
    //                    : (int)HttpStatusCode.Unauthorized;

    //                return;
    //            }

    //            dashboardContext.UriMatch = findResult.Item2;

    //            await findResult.Item1.Dispatch(dashboardContext);
    //        }
    //        finally
    //        {
    //            context.Request.PathBase = pathBase;
    //            context.Request.Path = path;
    //        }
    //    }
    //}
}