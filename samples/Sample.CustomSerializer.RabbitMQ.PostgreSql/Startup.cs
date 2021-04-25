using DotNetCore.CAP.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.CustomSerializer.Rabbit.PostgreSql.Domain;

namespace Sample.Kafka.PostgreSql
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
            string dbConnectionString = Configuration.GetValue<string>("ConnectionStrings:NpgsqlDbConnectionString");

            services.AddCap(cap =>
            {
                cap.UsePostgreSql(dbConnectionString);

                cap.UseRabbitMQ(r =>
                {
                    r.Port = 5672;
                    r.HostName = "127.0.0.1";
                    r.UserName = "guest";
                    r.Password = "guest";
                });

                cap.UseDashboard();

            });

            services.AddMessageSerializationProvider();

            /// Add custom serializer and genegic publisher
            services.AddPublisher<HanoiDto>();
            CapSerializerBuilder.AddMessageSerializer<HanoiDto, HanoiSerializer>();

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