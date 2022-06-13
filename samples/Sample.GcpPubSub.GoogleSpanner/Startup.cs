using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DotNetCore.CAP.GoogleSpanner;
using Microsoft.Extensions.Configuration;
using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.NodeDiscovery;

namespace Sample.GcpPubSub.GoogleSpanner
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
            const string connectionString = "Data Source=projects/digitalcore-sandbox/instances/sheeley-test/databases/cap";
  
            services.AddCap(x =>
            {
                x.UseGoogleSpanner(connectionString);
                x.UseGooglePubSub(cfg =>
                {
                    cfg.ProjectId = Configuration["Pubsub:ProjectId"];
                    cfg.SubscriptionId = Configuration["Pubsub:SubscriptionId"];
                    cfg.VerificationToken = Configuration["Pubsub:VerificationToken"];
                    cfg.TopicId = Configuration["Pubsub:TopicId"];
                });
                x.UseDashboard();
            });
            //services.AddSingleton<INodeDiscoveryProvider>();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}