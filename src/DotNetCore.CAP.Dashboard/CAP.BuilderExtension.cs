// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

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

                app.UseMiddleware<UiMiddleware>();

                app.Map(option.PathMatch + "/api", false, x =>
                {

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
}