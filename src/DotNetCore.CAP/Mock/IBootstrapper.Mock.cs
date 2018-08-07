using System.Threading.Tasks;

namespace DotNetCore.CAP.Mock
{
    internal class MockBootstrapper : IBootstrapper
    {
        public Task BootstrapAsync()
        {
            return Task.CompletedTask;
        }
    }
}