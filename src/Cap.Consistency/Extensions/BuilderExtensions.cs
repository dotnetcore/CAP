using System;
using Cap.Consistency;
using Cap.Consistency.Routing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Consistence extensions for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class BuilderExtensions
    {
        ///<summary>
        /// Enables Consistence for the current application
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
        public static IApplicationBuilder UseConsistency(this IApplicationBuilder app) {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            
            var marker = app.ApplicationServices.GetService<ConsistencyMarkerService>();
         
            if (marker == null) {
                throw new InvalidOperationException("Add Consistency must be called on the service collection.");
            }

            var router = app.ApplicationServices.GetService<ITopicRoute>();

            var context = new TopicRouteContext();

            router.RouteAsync(context).Wait();

            return app;
        }

    }
}