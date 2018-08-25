using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Kafka.SqlServer
{
    public class Startup
    {
        public const string ConnectionString = "Server=localhost;Integrated Security=SSPI;Database=testcap";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                x.UseSqlServer(ConnectionString);
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