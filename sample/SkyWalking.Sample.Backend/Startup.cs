using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkyWalking.AspNetCore;
using SkyWalking.Diagnostics.EntityFrameworkCore;
using SkyWalking.Sample.Backend.Models;

namespace SkyWalking.Sample.Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSkyWalking(option =>
            {
                option.DirectServers = "localhost:11800";
                option.ApplicationCode = "asp-net-core-backend";
            }).
            AddEntityFrameworkCore(c => { c.AddSqlite(); });

            var sqliteConnection = new SqliteConnection("DataSource=:memory:");
            sqliteConnection.Open();
            
            services.AddEntityFrameworkSqlite().AddDbContext<SampleDbContext>(c => c.UseSqlite(sqliteConnection));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            using (var scope = app.ApplicationServices.CreateScope())
            {
                using (var sampleDbContext = scope.ServiceProvider.GetService<SampleDbContext>())
                {
                    sampleDbContext.Database.EnsureCreated();
                } 
            }

            app.UseMvc();
        }
    }
}