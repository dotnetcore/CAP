using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Kafka.MySql
{
    public class Startup
    {
        public const string ConnectionString = "Server=localhost;Database=testcap;UserId=root;Password=123123;";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                x.UseMySql(ConnectionString);
                x.UseKafka("localhost:9092");
                x.UseDashboard();
            });

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
    }
}