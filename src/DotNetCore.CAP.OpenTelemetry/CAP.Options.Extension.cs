// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class OpenTelemetryCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapDiagnosticObserver>();
            services.AddSingleton<CapDiagnosticProcessorObserver>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, OpenTelemetryDiagnosticRegister>());
        }
    }

    public static class CapOptionsExtensions
    {
        public static CapOptions UseOpenTelemetry(this CapOptions options)
        {
            options.RegisterExtension(new OpenTelemetryCapOptionsExtension());

            return options;
        }
    }

    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Enables the message eventing data collection for CAP.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddCapInstrumentation(this TracerProviderBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddSource("DotNetCore.CAP.OpenTelemetry")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CAP"));

            return builder;
        }
    }
}