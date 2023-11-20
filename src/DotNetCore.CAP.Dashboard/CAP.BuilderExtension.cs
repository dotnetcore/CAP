// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

[assembly: InternalsVisibleTo("DotNetCore.CAP.Dashboard.K8s")]

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

public static class CapBuilderExtension
{
    private const string EmbeddedFileNamespace = "DotNetCore.CAP.Dashboard.wwwroot.dist";

    internal static IApplicationBuilder UseCapDashboard(this IApplicationBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        var provider = app.ApplicationServices;

        var options = provider.GetService<DashboardOptions>();

        if (options != null)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = options.PathMatch,
                FileProvider = new EmbeddedFileProvider(options.GetType().Assembly, EmbeddedFileNamespace)
            });

            var endpointRouteBuilder = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"]!;

            endpointRouteBuilder.MapGet(
                pattern: options.PathMatch, 
                requestDelegate: httpContext =>
            {
                var path = httpContext.Request.Path.Value;
                
                var redirectUrl = string.IsNullOrEmpty(path) || path.EndsWith("/")
                    ? "index.html"
                    : $"{path.Split('/').Last()}/index.html";
                
                httpContext.Response.StatusCode = 301;
                httpContext.Response.Headers["Location"] = redirectUrl;
                return Task.CompletedTask;
            }).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);

            endpointRouteBuilder.MapGet(
                pattern: options.PathMatch + "/index.html", 
                requestDelegate: async httpContext =>
            {
                httpContext.Response.StatusCode = 200;
                httpContext.Response.ContentType = "text/html;charset=utf-8";

                await using var stream = options.GetType().Assembly.GetManifestResourceStream(EmbeddedFileNamespace + ".index.html");
                
                if (stream == null) throw new InvalidOperationException();

                using var sr = new StreamReader(stream);
                var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
                htmlBuilder.Replace("%(servicePrefix)", options.PathBase + options.PathMatch + "/api");
                htmlBuilder.Replace("%(pollingInterval)", options.StatsPollingInterval.ToString());
                await httpContext.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
            }).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);

            new RouteActionProvider(endpointRouteBuilder, options).MapDashboardRoutes();
        }

        return app;
    }

    internal static IEndpointConventionBuilder AllowAnonymousIf(this IEndpointConventionBuilder builder, bool allowAnonymous, params string?[] authorizationPolicies)
    {
        if (allowAnonymous) return builder.AllowAnonymous();
        
        var validAuthorizationPolicies = authorizationPolicies
            .Where(policy => !string.IsNullOrEmpty(policy))!
            .ToArray<string>();
        
        if (!validAuthorizationPolicies.Any())
        {
            throw new InvalidOperationException("If Dashboard Options does not explicitly allow anonymous requests, the Authorization Policy must be configured.");
        }
        
        return builder.RequireAuthorization(validAuthorizationPolicies);
    }
}