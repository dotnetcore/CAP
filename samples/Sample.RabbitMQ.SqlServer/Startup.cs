using DotNetCore.CAP;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.RabbitMQ.SqlServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();

            //services
            //    .AddSingleton<IConsumerServiceSelector, TypedConsumerServiceSelector>()
            //    .AddQueueHandlers(typeof(Startup).Assembly);

            services.AddCap(x =>
            {
                x.UseEntityFramework<AppDbContext>();
                x.UseRabbitMQ(opt =>
                {
                    opt.HostName = "localhost";
                    opt.BasicQosOptions = new RabbitMQOptions.BasicQos(1);
                });
                x.UseDashboard();
                //x.ConsumerThreadCount = 4;
                x.EnableConsumerPrefetch = true;
                
                //x.FailedRetryCount = 5;
                //x.UseDispatchingPerGroup = true;
                //x.FailedThresholdCallback = failed =>
                //{
                //    var logger = failed.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                //    logger.LogError($@"A message of type {failed.MessageType} failed after executing {x.FailedRetryCount} several times, 
                //        requiring manual troubleshooting. Message name: {failed.Message.GetName()}");
                //};
                //x.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
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
