using Microsoft.Extensions.DependencyInjection;
using SkyWalking.Utilities.DependencyInjection;

namespace SkyWalking.AspNetCore.Diagnostics
{
    public static class SkyWalkingBuilderExtensions
    {
        public static SkyWalkingExtensions AddAspNetCoreHosting(this SkyWalkingExtensions extensions)
        {
            extensions.Services.AddSingleton<ITracingDiagnosticProcessor, HostingTracingDiagnosticProcessor>();
            return extensions;
        }
    }
}