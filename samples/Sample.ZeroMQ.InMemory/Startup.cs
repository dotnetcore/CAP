using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sample.ZeroMQ.InMemory;
using System;

namespace Sample.ZeroMQ.InMemory
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<ZeroMQService>();
            
            services.AddCap(x =>
            {
                x.UseInMemoryStorage();
                x.UseZeroMQ(cfg =>
                {
                    cfg.HostName = "127.0.0.1";
                    cfg.SubPort = 5556;
                    cfg.PubPort = 5557;
                    cfg.Pattern = DotNetCore.CAP.ZeroMQ.NetMQPattern.PushPull;

                });
                //x.UseRabbitMQ(cfg =>
                //{
                //    cfg.HostName = "172.17.124.92";
                //    cfg.UserName = "guest";
                //    cfg.Password = "guest";
                //});
                x.UseDashboard();
            });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}