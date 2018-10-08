using Microsoft.AspNetCore.Hosting;
using SkyWalking.Agent.AspNetCore;

[assembly: HostingStartup(typeof(SkyWalkingHostingStartup))]

namespace SkyWalking.Agent.AspNetCore
{
    internal class SkyWalkingHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => services.AddSkyWalkingCore());
        }
    }
}