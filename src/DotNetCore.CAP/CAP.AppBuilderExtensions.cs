using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.GatewayProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// app extensions for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class AppBuilderExtensions
    {
        ///<summary>
        /// Enables cap for the current application
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
        public static IApplicationBuilder UseCap(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var marker = app.ApplicationServices.GetService<CapMarkerService>();

            if (marker == null)
            {
                throw new InvalidOperationException("Add Cap must be called on the service collection.");
            }

            var provider = app.ApplicationServices;
            var bootstrapper = provider.GetRequiredService<IBootstrapper>();
            bootstrapper.BootstrapAsync();
            return app;
        }

        public static IApplicationBuilder UseCapDashboard(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));          

            var marker = app.ApplicationServices.GetService<CapMarkerService>();

            if (marker == null)
            {
                throw new InvalidOperationException("Add Cap must be called on the service collection.");
            }

            app.UseMiddleware<GatewayProxyMiddleware>();
            app.UseMiddleware<DashboardMiddleware>();
           
            return app;
        }
    }
}