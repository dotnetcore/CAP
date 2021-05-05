using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Sample.RabbitMQ.Postgres.DashboardAuth
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(cap =>
           {
               cap.UseInMemoryStorage();
               cap.UseDashboard();
               cap.UseInMemoryMessageQueue();
           });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            //app.UseAuthentication();
            app.UseRouting();
            //app.UseAuthorization();

            app.UseRequestLocalization(app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value);
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}