using Microsoft.AspNetCore.Hosting;
using SkyWalking.AspNetCore;

[assembly: HostingStartup(typeof(SkyWalkingHostingStartup))]

namespace SkyWalking.AspNetCore
{
    public class SkyWalkingHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => services.AddSkyWalkingCore());
        }
    }
}