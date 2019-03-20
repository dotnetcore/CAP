// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// app extensions for <see cref="IApplicationBuilder" />
    /// </summary>
    internal static class AppBuilderExtensions
    {
        /// <summary>
        /// Enables cap for the current application
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder" /> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder" /> instance this method extends.</returns>
        public static IApplicationBuilder UseCapDashboard(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            CheckRequirement(app);

            var provider = app.ApplicationServices;
             
            if (provider.GetService<DashboardOptions>() != null)
            {
                if (provider.GetService<DiscoveryOptions>() != null)
                {
                    app.UseMiddleware<GatewayProxyMiddleware>();
                }

                app.UseMiddleware<DashboardMiddleware>();
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
                app.UseCapDashboard();

                next(app);
            };
        }
    }
}