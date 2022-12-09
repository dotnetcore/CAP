// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public static class CapBuilderExtension
    {
        internal static IApplicationBuilder UseCapDashboard(this IApplicationBuilder app)
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

                var endPointRouteBuilder = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"]!;

                new RouteActionProvider(endPointRouteBuilder, options).MapDashboardRoutes();
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
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return false;
                }
            }

            return true;
        }

        internal static async Task<bool> Authorize(HttpContext httpContext, DashboardOptions options)
        {
            IAuthorizationService authService = null;
            if (!string.IsNullOrEmpty(options.AuthorizationPolicy))
            {
                authService = httpContext.RequestServices.GetService<IAuthorizationService>();
            }
            if (!string.IsNullOrEmpty(options.AuthorizationPolicy) && (authService != null))
            {
                AuthorizationResult authorizationResult = await authService.AuthorizeAsync(httpContext.User, null, options.AuthorizationPolicy);
                if (!authorizationResult.Succeeded)
                {
                    return false;
                }
            }
            return true;
        }
    }
}