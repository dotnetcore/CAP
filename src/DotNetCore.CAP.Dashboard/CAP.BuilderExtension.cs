// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public static class CapBuilderExtension
    {
        private const string EmbeddedFileNamespace = "DotNetCore.CAP.Dashboard.wwwroot.dist";

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
                app.UseStaticFiles(new StaticFileOptions()
                {
                    RequestPath = options.PathMatch,
                    FileProvider = new EmbeddedFileProvider(options.GetType().Assembly, EmbeddedFileNamespace)
                });

                var endPointRouteBuilder = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"]!;

                endPointRouteBuilder.MapGet(options.PathMatch, httpContext =>
                {
                    var path = httpContext.Request.Path.Value;
                    var redirectUrl = string.IsNullOrEmpty(path) || path.EndsWith("/") ? "index.html" : $"{path.Split('/').Last()}/index.html";
                    httpContext.Response.StatusCode = 301;
                    httpContext.Response.Headers["Location"] = redirectUrl;
                    return Task.CompletedTask;
                }).AllowAnonymousIf(options.AllowAnonymousExplicit);

                endPointRouteBuilder.MapGet(options.PathMatch + "/index.html", async httpContext =>
                {
                    if (!await Authentication(httpContext, options))
                    {
                        if (httpContext.Response.StatusCode != StatusCodes.Status302Found)
                            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    httpContext.Response.StatusCode = 200;
                    httpContext.Response.ContentType = "text/html;charset=utf-8";

                    await using var stream = options.GetType().Assembly.GetManifestResourceStream(EmbeddedFileNamespace + ".index.html");
                    if (stream == null) throw new InvalidOperationException();

                    using var sr = new StreamReader(stream);
                    var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
                    htmlBuilder.Replace("%(servicePrefix)", options.PathBase + options.PathMatch + "/api");
                    htmlBuilder.Replace("%(pollingInterval)", options.StatsPollingInterval.ToString());
                    await httpContext.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                }).AllowAnonymousIf(options.AllowAnonymousExplicit);

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

        internal static IEndpointConventionBuilder AllowAnonymousIf(this IEndpointConventionBuilder builder, bool allowAnonymous)
        {
            return allowAnonymous ? builder.AllowAnonymous() : builder;
        }
    }
}