using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.RabbitMQ.MySql
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();

            services.AddCap(x =>
            {
                /*
                x.UseEntityFramework<AppDbContext>();
                x.UseRabbitMQ("localhost");
                x.UseDashboard();
                x.FailedRetryCount = 5;
                x.FailedThresholdCallback = failed =>
                {
                    var logger = failed.ServiceProvider.GetService<ILogger<Startup>>();
                    logger.LogError($@"A message of type {failed.MessageType} failed after executing {x.FailedRetryCount} several times,
                        requiring manual troubleshooting. Message name: {failed.Message.GetName()}");
                };
                */

                //如果你使用的ADO.NET，根据数据库选择进行配置：
                x.UseMySql("Server=192.168.16.150;Port=3306;Database=order;Uid=uid;Pwd=pwd;Charset=utf8mb4");

                //CAP支持 RabbitMQ、Kafka、AzureServiceBus 等作为MQ，根据使用选择配置：
                x.UseRabbitMQ(cfg =>
                {
                    cfg.HostName = "192.168.16.150";
                    cfg.VirtualHost = "dev";
                    cfg.Port = 5672;
                    cfg.UserName = "dev";
                    cfg.Password = "password";
                    cfg.ExchangeName = "ex.delayed.message";
                    cfg.ExChangeType = "x-delayed-message"; // 延迟队列
                });
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