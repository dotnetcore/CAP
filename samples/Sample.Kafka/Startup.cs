using DotNetCore.CAP.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.Kafka
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();

            services.AddCap()
                    .AddEntityFrameworkStores<AppDbContext>(x=> {
                        //x.ConnectionString = "Server=192.168.2.206;Initial Catalog=Test;User Id=cmswuliu;Password=h7xY81agBn*Veiu3;MultipleActiveResultSets=True";
                        x.ConnectionString = "Server=DESKTOP-M9R8T31;Initial Catalog=Test;User Id=sa;Password=P@ssw0rd;MultipleActiveResultSets=True";
                    })
                    .AddRabbitMQ(x =>
                    {
                        x.HostName = "localhost";
                       // x.UserName = "admin";
                       // x.Password = "123123";
                    });
            //.AddKafka(x => x.Servers = "");
            
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();

            app.UseCap();
        }
    }
}