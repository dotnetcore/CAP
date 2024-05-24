﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Kafka.PostgreSql
{
    public class Startup
    {
        public const string DbConnectionString = "User ID=postgres;Password=mysecretpassword;Host=127.0.0.1;Port=5432;Database=postgres;";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                //docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres
                x.UsePostgreSql(DbConnectionString);

                //docker run --name kafka -p 9092:9092 -d bashj79/kafka-kraft
                x.UseKafka("localhost:9092");
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