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
                x.UseRabbitMQ(z =>
                {
                    z.HostName = "192.168.2.206";
                    z.UserName = "admin";
                    z.Password = "123123";
                });
                x.UseDashboard(d => { d.StatsPollingInterval = 1000000; });
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            app.UseMvc();

            app.UseCap();
        }
    }
}