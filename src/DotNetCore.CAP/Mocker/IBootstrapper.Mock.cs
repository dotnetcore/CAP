using System.Threading.Tasks;

namespace DotNetCore.CAP.Mocker
{
    internal class MockBootstrapper : IBootstrapper
    {
        public Task BootstrapAsync()
        {
            return Task.CompletedTask;
        }
    }
}