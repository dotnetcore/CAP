using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            services.AddSingleton<IMongoClient>(new MongoClient(Configuration.GetConnectionString("MongoDB")));
            services.AddCap(x =>
            {
                x.UseMongoDB();

                var mq = new RabbitMQOptions();
                Configuration.GetSection("RabbitMQ").Bind(mq);
                x.UseRabbitMQ(cfg =>
                {
                    cfg.HostName = mq.HostName;
                    cfg.Port = mq.Port;
                    cfg.UserName = mq.UserName;
                    cfg.Password = mq.Password;
                });

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

            app.UseCap();
        }
    }
}
