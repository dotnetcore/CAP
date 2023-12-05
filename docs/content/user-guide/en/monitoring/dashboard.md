# Dashboard

CAP provides a Dashboard for viewing messages, and features provided by Dashboard make it easy to view and manage messages.

!!! WARNING "Usage Limit"
    The Dashboard is only supported for use in ASP.NET Core, Not supported for console application
    
## Enable Dashboard

By default, Dashboard middleware will not be launched. To enable Dashboard functionality you need to add the following code to your configuration:

```C#
services.AddCap(x =>
{
    //...

    // Register Dashboard
    x.UseDashboard();
});
```

By default, you can open the Dashboard by visiting the url `http://localhost:xxx/cap`.

### Dashboard Configuration

* PathMatch

> Default ：'/cap'

You can change the path of the Dashboard by modifying this configuration option.

* StatsPollingInterval

> Default: 2000ms

This configuration option is used to configure the Dashboard front end to get the polling time of the status interface (/stats).

* AllowAnonymousExplicit

> Default: true

Explicitly allows anonymous access for the CAP dashboard API, passing AllowAnonymous to the ASP.NET Core global authorization filter.

* AuthorizationPolicy

> Default: null.

Authorization policy for the Dashboard. Required if `AllowAnonymousExplicit` is false.

###  Custom Authentication

From version 8.0.0, the CAP Dashboard leverages ASP.NET Core authentication mechanisms allowing extensibility through custom authorization policies and ASP.NET Core authentication and authorization middlewares to authorize Dashboard access. For more details of ASP.NET Core authentication internals, check [the official docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0).

You can view the examples below in the sample project `Sample.Dashboard.Auth`.

#### Example: Anonymous Access

```csharp
services.AddCap(cap =>
    {
        cap.UseDashboard(d =>
        {
            d.AllowAnonymousExplicit = true;
        });
        cap.UseInMemoryStorage();
        cap.UseInMemoryMessageQueue();
    });
```

#### Example: Open Id

```csharp
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
            ...
        });
    
    services.AddCap(cap =>
    {
        cap.UseDashboard(d =>
        {
            d.AuthorizationPolicy = DashboardAuthorizationPolicy;
        });
        cap.UseInMemoryStorage();
        cap.UseInMemoryMessageQueue();
    });
```

#### Example: Custom Authentication Scheme

```csharp
const string MyDashboardAuthenticationPolicy = "MyDashboardAuthenticationPolicy";
    
services.AddAuthorization(options =>
    { 
        options.AddPolicy(MyDashboardAuthenticationPolicy, policy => policy
        .AddAuthenticationSchemes(MyDashboardAuthenticationSchemeDefaults.Scheme)
        .RequireAuthenticatedUser());
    })
    .AddAuthentication()
    .AddScheme<MyDashboardAuthenticationSchemeOptions, MyDashboardAuthenticationHandler>(MyDashboardAuthenticationSchemeDefaults.Scheme,null);
    
services.AddCap(cap =>
    {
        cap.UseDashboard(d =>
        {
            d.AuthorizationPolicy = MyDashboardAuthenticationPolicy;
        });
        cap.UseInMemoryStorage();
        cap.UseInMemoryMessageQueue();
    });
```
