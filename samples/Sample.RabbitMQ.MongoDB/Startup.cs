using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Sample.RabbitMQ.MongoDB
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMongoClient>(new MongoClient("mongodb://192.168.10.110:27017,192.168.10.110:27018,192.168.10.110:27019/?replicaSet=rs0"));
            services.AddCap(x =>
            {
                x.UseMongoDB("mongodb://192.168.10.110:27017,192.168.10.110:27018,192.168.10.110:27019/?replicaSet=rs0");
                x.UseRabbitMQ("localhost");
                x.UseDashboard();
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(); 
        }
    }
}
