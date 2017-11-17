using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.RabbitMQ.SqlServer;

namespace Sample.Kafka.SqlServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();

            services.AddCap(x =>
            {
                x.UseEntityFramework<AppDbContext>();
                x.UseKafka("192.168.2.215:9092");
                x.UseDashboard();
                //x.UseDiscovery(d =>
                //{
                //    d.DiscoveryServerHostName = "localhost";
                //    d.DiscoveryServerPort = 8500;
                //    d.CurrentNodeHostName = "localhost";
                //    d.CurrentNodePort = 5820;
                //    d.NodeName = "CAP 2号节点";
                //});
            }).AddMessagePacker<MyMessagePacker>();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMvc();

            app.UseCap();
        }
    }
}