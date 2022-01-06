// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.OpenTelemetry;

// ReSharper disable once CheckNamespace
namespace OpenTelemetry.Trace
{
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

            builder.AddSource(DiagnosticListener.SourceName);

            var instrumentation = new CapInstrumentation(new DiagnosticListener());

            return builder.AddInstrumentation(() => instrumentation);
        }
    }
}