using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.RabbitMQ.PostgreSql
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                x.UseEntityFramework<AppDbContext>();
                x.UseRabbitMQ(z =>
                {
                    z.HostName = "192.168.2.206";
                    z.UserName = "admin";
                    z.Password = "123123";
                });
                x.UseDashboard();
                x.UseDiscovery(d =>
                {
                    d.DiscoveryServerHostName = "localhost";
                    d.DiscoveryServerProt = 8500;
                    d.CurrentNodeHostName = "localhost";
                    d.CurrentNodePort = 5800;
                    d.NodeName = "CAP一号节点";
                });
            });
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();

            app.UseCap();
        }
    }
}
