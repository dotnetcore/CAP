using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers;
using Serilog;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(x => x.AddSerilog());

            services
                .AddSingleton<IConsumerServiceSelector, TypedConsumerServiceSelector>()
                .AddQueueHandlers(typeof(Startup).Assembly);

            services.AddCap(options =>
            {
                options.UseSqlServer("Server=(local);Database=CAP-Test;Trusted_Connection=True;");
                options.UseRabbitMQ("localhost");
                options.UseDashboard();
                options.GroupNamePrefix = "th";
                options.ConsumerThreadCount = 1;

                options.UseDispatchingPerGroup = true;
            });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseSerilogRequestLogging();
            app.UseCapDashboard();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
