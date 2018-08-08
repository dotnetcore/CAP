using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensionsMocker
    {
        public static CapOptions UseSqlServer(this CapOptions options)
        {
            options.RegisterExtension(new MockSqlServerCapOptionsExtension());

            return options;
        }

        public static CapOptions UseMockEntityFramework(this CapOptions options)
        {
            options.RegisterExtension(new MockSqlServerCapOptionsExtension());

            return options;
        }
    }

    internal class MockSqlServerCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton(new SqlServerOptions());
        }
    }
}