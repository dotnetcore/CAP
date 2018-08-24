using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Kafka.MySql
{
    public class Startup
    {
        public const string ConnectionString = "Server=localhost;Database=testcap;UserId=root;Password=123123;";
        //public const string ConnectionString = "Server=(localdb)\\ProjectsV13;Integrated Security=SSPI;Database=testcap";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                x.UseMySql(ConnectionString);
                //x.UseSqlServer(ConnectionString);
                x.UseKafka("192.168.10.110:9092");
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