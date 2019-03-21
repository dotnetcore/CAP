using Microsoft.Extensions.DependencyInjection;
using SkyApm.Utilities.DependencyInjection;
using System;

namespace SkyApm.Diagnostics.SmartSql
{
    public static class SkyWalkingBuilderExtensions
    {
        public static SkyApmExtensions AddSmartSql(this SkyApmExtensions extensions)
        {
            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            extensions.Services.AddSingleton<ITracingDiagnosticProcessor, SmartSqlTracingDiagnosticProcessor>();

            return extensions;
        }
    }
}
