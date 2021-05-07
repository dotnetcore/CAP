using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Sample.Dashboard.Blazor
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
            services.AddBootstrapBlazor();
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
            app.UseRouting();

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