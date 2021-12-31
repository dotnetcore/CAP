// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class OpenTelemetryCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
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
}