using Microsoft.AspNetCore.Hosting;

namespace SkyWalking.Agent.AspNetCore
{
    internal class HostingEnvironmentProvider : IEnvironmentProvider
    {
        public string EnvironmentName { get; }

        public HostingEnvironmentProvider(IHostingEnvironment hostingEnvironment)
        {
            EnvironmentName = hostingEnvironment.EnvironmentName;
        }
    }
}