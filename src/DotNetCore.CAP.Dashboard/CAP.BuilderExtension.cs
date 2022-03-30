// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

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

            var provider = app.ApplicationServices;

            var options = provider.GetService<DashboardOptions>();
            if (options != null)
            {
                if (provider.GetService<DiscoveryOptions>() != null)
                {
                    app.UseMiddleware<GatewayProxyMiddleware>();
                }

                app.UseMiddleware<UiMiddleware>();

                app.Map(options.PathMatch + "/api", false, x =>
                {

                    IAuthorizationService authService = null;
                    if (!String.IsNullOrEmpty(options.AuthorizationPolicy))
                    {
                        authService = app.ApplicationServices.GetService<IAuthorizationService>();
                    }

                    var builder = new RouteBuilder(x);

                    var methods = typeof(RouteActionProvider).GetMethods(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var method in methods)
                    {
                        var executor = ObjectMethodExecutor.Create(method, typeof(RouteActionProvider).GetTypeInfo());

                        var getAttr = method.GetCustomAttribute<HttpGetAttribute>();
                        if (getAttr != null)
                        {

                            builder.MapGet(getAttr.Template, async (request, response, data) =>
                            {
                                if (!await Authentication(request.HttpContext, options))
                                {
                                    response.StatusCode = StatusCodes.Status401Unauthorized;
                                    return;
                                }

                                if (!await Authorize(request, response, options, authService))
                                {
                                    response.StatusCode = StatusCodes.Status401Unauthorized;
                                    return;
                                }

                                var actionProvider = new RouteActionProvider(request, response, data);
                                try
                                {
                                    await executor.ExecuteAsync(actionProvider, null);
                                }
                                catch (Exception ex)
                                {
                                    response.StatusCode = StatusCodes.Status500InternalServerError;
                                    await response.WriteAsync(ex.Message);
                                }
                            });
                        }

                        var postAttr = method.GetCustomAttribute<HttpPostAttribute>();
                        if (postAttr != null)
                        {
                            builder.MapPost(postAttr.Template, async (request, response, data) =>
                            {
                                if (!await Authentication(request.HttpContext, options))
                                {
                                    response.StatusCode = StatusCodes.Status401Unauthorized;
                                    return;
                                }

                                if (!await Authorize(request, response, options, authService))
                                {
                                    response.StatusCode = StatusCodes.Status401Unauthorized;
                                    return;
                                }

                                var actionProvider = new RouteActionProvider(request, response, data);
                                try
                                {
                                    await executor.ExecuteAsync(actionProvider, null);
                                }
                                catch (Exception ex)
                                {
                                    response.StatusCode = StatusCodes.Status500InternalServerError;
                                    await response.WriteAsync(ex.Message);
                                }
                            });
                        }
                    }

                    var capRouter = builder.Build();

                    x.UseRouter(capRouter);
                });
            }

            return app;
        }

        internal static async Task<bool> Authentication(HttpContext context, DashboardOptions options)
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated;

            if (isAuthenticated == false && options.UseChallengeOnAuth)
            {
                await context.ChallengeAsync(options.DefaultChallengeScheme);
                await context.Response.CompleteAsync();

                return false;
            }

            if (isAuthenticated == false && options.UseAuth)
            {
                var result = await context.AuthenticateAsync(options.DefaultAuthenticationScheme);

                if (result.Succeeded && result.Principal != null)
                {
                    //If a cookie scheme is configured, the authentication result will be placed in a cookie to avoid re-authentication
                    var defaultOptions = context.RequestServices.GetService<IOptions<AuthenticationOptions>>();
                    if (defaultOptions != null && defaultOptions.Value.DefaultScheme == CookieAuthenticationDefaults.AuthenticationScheme)
                    {
                        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        internal static async Task<bool> Authorize(HttpRequest request, HttpResponse response, DashboardOptions options, IAuthorizationService authservice)
        {
            if (!String.IsNullOrEmpty(options.AuthorizationPolicy) && (authservice != null))
            {
                AuthorizationResult authorizationResult = await authservice.AuthorizeAsync(request.HttpContext.User, null, options.AuthorizationPolicy);
                if (!authorizationResult.Succeeded)
                {
                    return false;
                }
            }
            return true;
        }
    }
}