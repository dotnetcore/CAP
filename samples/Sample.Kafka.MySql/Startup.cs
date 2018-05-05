using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Kafka.MySql
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                x.UseMySql("Server=192.168.10.110;Database=testcap;UserId=root;Password=123123;");
                x.UseKafka("192.168.10.110:9092");
                x.UseDashboard();
            });

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc();

            app.UseCap();
        }
    }
}