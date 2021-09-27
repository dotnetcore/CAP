using System;
using System.Threading;
using DotNetCore.CAP.Test.FakeInMemoryQueue;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Test.Helpers
{
    public static class TestServiceCollectionExtensions
    {
        public const string TestGroupName = "Test";

        public static void AddTestSetup(this IServiceCollection services, ITestOutputHelper testOutput)
        {
            services.AddLogging(x => x.AddTestLogging(testOutput));
            services.AddCap(x =>
            {
                x.DefaultGroupName = TestGroupName;
                x.UseFakeTransport();
                x.UseInMemoryStorage();
            });
            services.AddHostedService<TestBootstrapService>();
        }

        public static ServiceProvider BuildTestContainer(this IServiceCollection services, CancellationToken cancellationToken)
        {
            var container = services.BuildServiceProvider();
            container.StartHostedServices(cancellationToken);
            return container;
        }
    }
}