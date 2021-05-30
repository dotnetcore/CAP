using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Dashboard.Auth
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

            services.AddCors(x =>
            {
                x.AddDefaultPolicy(p =>
                {
                    p.WithOrigins("http://localhost:8080").AllowCredentials().AllowAnyHeader().AllowAnyMethod();
                });
            });
      
            services.AddCap(cap =>
            {
                cap.UseDashboard(d =>
                {
                    d.UseChallengeOnAuth = true;
                    d.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                });
                cap.UseMySql(_configuration.GetValue<string>("ConnectionString"));
                cap.UseRabbitMQ(aa =>
                {
                    aa.HostName = "192.168.3.57";
                    aa.UserName = "user";
                    aa.Password = "wJ0p5gSs17";
                });
                //cap.UseDiscovery(_ =>
                //{
                //    _.DiscoveryServerHostName = "localhost";
                //    _.DiscoveryServerPort = 8500;
                //    _.CurrentNodeHostName = _configuration.GetValue<string>("ASPNETCORE_HOSTNAME");
                //    _.CurrentNodePort = _configuration.GetValue<int>("ASPNETCORE_PORT");
                //    _.NodeId = _configuration.GetValue<string>("NodeId");
                //    _.NodeName = _configuration.GetValue<string>("NodeName");
                //});
            });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseCors();
            app.UseRouting();
            app.UseAuthorization();
            app.UseCookiePolicy();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); 
            });
        }
    } 

}