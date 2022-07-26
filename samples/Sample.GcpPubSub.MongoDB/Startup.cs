using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

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
            services.AddSingleton<IMongoClient>(new MongoClient(Configuration.GetConnectionString("MongoDB")));

            services.AddCap(x =>
            {
                x.UseMongoDB(Configuration.GetConnectionString("MongoDB"));
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