using System;
using System.Collections.ObjectModel;
using System.Threading;
using DotNetCore.CAP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Test.IntegrationTests
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(10));
        protected readonly ServiceProvider Container;
        protected readonly ObservableCollection<object> HandledMessages = new();
        protected readonly ICapPublisher Publisher;

        protected IntegrationTestBase(ITestOutputHelper testOutput)
        {
            var services = new ServiceCollection();
            services.AddTestSetup(testOutput);
            services.AddSingleton(sp => new TestMessageCollector(HandledMessages));
            ConfigureServices(services);

            Container = services.BuildTestContainer(CancellationToken);
            Scope = Container.CreateScope();
            Publisher = Scope.ServiceProvider.GetRequiredService<ICapPublisher>();
        }

        protected IServiceScope Scope { get; }

        protected CancellationToken CancellationToken => CancellationTokenSource.Token;

        public void Dispose()
        {
            Scope?.Dispose();
            Container?.Dispose();
        }

        protected abstract void ConfigureServices(IServiceCollection services);
    }
}