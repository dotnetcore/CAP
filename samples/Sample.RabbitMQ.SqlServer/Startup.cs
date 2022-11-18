using System.Text.Encodings.Web;
using System.Text.Unicode;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.RabbitMQ.SqlServer.TypedConsumers;

namespace Sample.RabbitMQ.SqlServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();

            services
                .AddSingleton<IConsumerServiceSelector, TypedConsumerServiceSelector>()
                .AddQueueHandlers(typeof(Startup).Assembly);

            services.AddCap(x =>
            {
                x.UseEntityFramework<AppDbContext>();
                x.UseRabbitMQ(y =>
                {
                    y.UserName = "user";
                    y.Password = "pass";
                    y.HostName = "localhost:5672,localhost:5673,localhost:5674";
                });
                x.UseDashboard();
                x.FailedRetryCount = 5;
                x.UseDispatchingPerGroup = true;
                x.FailedThresholdCallback = failed =>
                {
                    var logger = failed.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                    logger.LogError($@"A message of type {failed.MessageType} failed after executing {x.FailedRetryCount} several times, 
                        requiring manual troubleshooting. Message name: {failed.Message.GetName()}");
                };
                x.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
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
