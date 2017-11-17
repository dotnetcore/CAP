using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.RabbitMQ.SqlServer.Services;
using Sample.RabbitMQ.SqlServer.Services.Impl;

namespace Sample.RabbitMQ.SqlServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddTransient<ICmsService, CmsService>();

            services.AddCap(x =>
            {
                x.UseEntityFramework<AppDbContext>();
                x.UseRabbitMQ("localhost");
                x.UseDashboard();
                x.UseDiscovery(d =>
                {
                    d.DiscoveryServerHostName = "localhost";
                    d.DiscoveryServerPort = 8500;

                    d.CurrentNodeHostName = "192.168.1.11";
                    d.CurrentNodePort = 5800;
                    d.NodeName = "CAP Node Windows";
                    d.NodeId = 1;
                });
            });

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            app.UseMvc();

            app.UseCap();
        }
    }
}