using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkyApm.Sample.Backend.Models;
using SkyApm.Sample.Backend.Sampling;
using SkyApm.Tracing;
using SmartSql;
using SmartSql.DataSource;

namespace SkyApm.Sample.Backend
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

            var sqliteConnection = new SqliteConnection("DataSource=:memory:");
            sqliteConnection.Open();

            services.AddEntityFrameworkSqlite().AddDbContext<SampleDbContext>(c => c.UseSqlite(sqliteConnection));

            services.AddSingleton<ISamplingInterceptor, CustomSamplingInterceptor>();
            services.AddSmartSql(sp =>
            {
                return SmartSqlBuilder
                .AddDataSource(DbProvider.SQLSERVER, "Data Source=.;Initial Catalog=SmartSqlTestDB;Integrated Security=True")
                .UseLoggerFactory(sp.GetService<ILoggerFactory>())
                .UseCache(false)
                .Build();
            });
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