namespace Sample.RabbitMQ.DM
{
    public class Startup
    {
        public const string ConnectionString = "Server=localhost;User Id=SYSDBA;PWD=Chang@2025;DATABASE=DAMENG";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCap(x =>
            {
                //x.UseStorageLock=true;
                x.UseDM(ConnectionString);
                x.UseRabbitMQ("localhost");
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
