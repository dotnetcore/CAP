using System.Net.Sockets;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sample.Kafka.PostgreSql
{
    public class Startup
    {
        public const string DbConnectionString = "User ID=postgres;Password=mysecretpassword;Host=127.0.0.1;Port=5432;Database=postgres;";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>((sp, opt) =>
            {
                opt.UseNpgsql(DbConnectionString)
                 .ReplaceService<IRelationalConnection, CapNpgsqlRelationalConnection>();
            });

            services.AddCap(x =>
            {
                //x.UseEntityFramework<AppDbContext>();
                //docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres
                x.UsePostgreSql(DbConnectionString);

                /* //Run Kafka Docker Container (Powershell)
                docker run -d `
                    --name kafka `
                    -p 9092:9092 `
                    -e KAFKA_NODE_ID=1 `
                    -e KAFKA_PROCESS_ROLES=broker,controller `
                    -e KAFKA_LISTENERS=PLAINTEXT://0.0.0.0:9092,CONTROLLER://:9093 `
                    -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://127.0.0.1:9092 `
                    -e KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER `
                    -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT `
                    -e KAFKA_CONTROLLER_QUORUM_VOTERS=1@localhost:9093 `
                    -e KAFKA_LOG_DIRS=/var/lib/kafka/data `
                    -e KAFKA_AUTO_CREATE_TOPICS_ENABLE=true `
                    -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 `
                    -e KAFKA_OFFSETS_TOPIC_MIN_ISR=1 `
                    -e KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1 `
                    -e KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1 `
                    apache/kafka:3.7.0
                */
                x.UseKafka("127.0.0.1:9092");
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