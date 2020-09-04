using Amazon;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Sample.AmazonSQS.InMemory
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                x.UseInMemoryStorage();
                x.UseInMemoryMessageQueue();
                x.UseDashboard();
            });

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