using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Sample.Dashboard.Auth;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // AddCapWithOpenIdAuthorization(services);
        // AddCapWithAnonymousAccess(services);
        // AddCapWithCustomAuthorization(services);
        AddCapWithOpenIdAndCustomAuthorization(services);

        services.AddCors(x =>
        {
            x.AddDefaultPolicy(p =>
            {
                p.WithOrigins("https://localhost:5001").AllowCredentials().AllowAnyHeader().AllowAnyMethod();
            });
        });

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseCors();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCookiePolicy();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

    private IServiceCollection AddCapWithOpenIdAuthorization(IServiceCollection services)
    {
        const string DashboardAuthorizationPolicy = "DashboardAuthorizationPolicy";

        services
            .AddAuthorization(options =>
            {
                options.AddPolicy(DashboardAuthorizationPolicy, policy => policy
                    .AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser());
            })
            .AddAuthentication(opt => opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.RequireHttpsMetadata = false;
                options.Authority = "https://demo.duendesoftware.com/";
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
            cap.UseDashboard(d =>
            {
                d.AllowAnonymousExplicit = false;
                d.AuthorizationPolicy = DashboardAuthorizationPolicy;
            });
            cap.UseInMemoryStorage();
            cap.UseInMemoryMessageQueue();
        });

        return services;
    }

    private IServiceCollection AddCapWithCustomAuthorization(IServiceCollection services)
    {
        const string MyDashboardAuthenticationPolicy = "MyDashboardAuthenticationPolicy";

        services
            .AddAuthorization(options =>
            {
                options.AddPolicy(MyDashboardAuthenticationPolicy, policy => policy
                    .AddAuthenticationSchemes(MyDashboardAuthenticationSchemeDefaults.Scheme)
                    .RequireAuthenticatedUser());
            })
            .AddAuthentication()
            .AddScheme<MyDashboardAuthenticationSchemeOptions, MyDashboardAuthenticationHandler>(MyDashboardAuthenticationSchemeDefaults.Scheme, null);

        services.AddCap(cap =>
        {
            cap.UseDashboard(d =>
            {
                d.AuthorizationPolicy = MyDashboardAuthenticationPolicy;
            });
            cap.UseInMemoryStorage();
            cap.UseInMemoryMessageQueue();
        });

        return services;
    }

    private IServiceCollection AddCapWithOpenIdAndCustomAuthorization(IServiceCollection services)
    {
        const string DashboardAuthorizationPolicy = "DashboardAuthorizationPolicy";

        services
            .AddAuthorization(options =>
            {
                options.AddPolicy(DashboardAuthorizationPolicy, policy => policy
                    .AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme, MyDashboardAuthenticationSchemeDefaults.Scheme)
                    .RequireAuthenticatedUser());
            })
            .AddAuthentication(opt => opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<MyDashboardAuthenticationSchemeOptions, MyDashboardAuthenticationHandler>(MyDashboardAuthenticationSchemeDefaults.Scheme, null)
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.RequireHttpsMetadata = false;
                options.Authority = "https://demo.duendesoftware.com/";
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
            cap.UseDashboard(d =>
            {
                d.AllowAnonymousExplicit = false;
                d.AuthorizationPolicy = DashboardAuthorizationPolicy;
            });
            cap.UseInMemoryStorage();
            cap.UseInMemoryMessageQueue();
        });

        return services;
    }

    private IServiceCollection AddCapWithAnonymousAccess(IServiceCollection services)
    {
        services.AddCap(cap =>
        {
            cap.UseDashboard(d =>
            {
                d.AllowAnonymousExplicit = true;
            });
            cap.UseInMemoryStorage();
            cap.UseInMemoryMessageQueue();
        });

        return services;
    }
}