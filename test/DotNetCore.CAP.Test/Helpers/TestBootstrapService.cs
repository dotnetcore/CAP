using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Hosting;

namespace DotNetCore.CAP.Test.Helpers
{
    public class TestBootstrapService : IHostedService
    {
        private readonly IBootstrapper _bootstrapper;

        public TestBootstrapService(IBootstrapper bootstrapper)
        {
            _bootstrapper = bootstrapper;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bootstrapper.BootstrapAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}