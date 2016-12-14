using Cap.Consistency;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// KafkaConsostence extensions for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Enables KafkaConsistence for the current application
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
        public static IApplicationBuilder UseKafkaConsistence(this IApplicationBuilder app) {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }

            var marker = app.ApplicationServices.GetService<KafkaConsistenceMarkerService>();
            if (marker == null) {
                throw new InvalidOperationException("AddKafkaConsistence must be called on the service collection.");
            }

            return app;
        }
    }
}