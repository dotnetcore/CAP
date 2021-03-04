using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.RabbitMQ.Postgres.DashboardAuth
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            
            services
                .AddAuthorization()
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(options =>
                {
                    options.Authority = "https://demo.identityserver.io/";
                    options.ClientId = "interactive.confidential";
                    options.ClientSecret = "secret";
                    options.ResponseType = "code";
                    options.UsePkce = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                });
            
            services.AddCap(cap =>
            {
                cap.UsePostgreSql(p =>
                {
                    p.ConnectionString = _configuration.GetConnectionString("Postgres");
                });

                /*
                 * Use the command below to start a rabbitmq instance locally:
                 * docker run -d --name rabbitmq -p 15672:15672 -p 5672:5672 rabbitmq:management
                 */
                cap.UseRabbitMQ(r =>
                {
                    r.Port = 5672;
                    r.HostName = "127.0.0.1";
                    r.UserName = "guest";
                    r.Password = "guest";
                });

                cap.UseDashboard(d =>
                {
                    d.UseChallengeOnAuth = true;
                    d.Authorization = new[] {new HttpContextDashboardFilter()};
                });
            });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}