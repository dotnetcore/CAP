# Dashboard

CAP provides a Dashboard for viewing messages. The features provided by the Dashboard make it easy to view and manage messages.

!!! WARNING "Usage Limit"
    The Dashboard is only supported for ASP.NET Core. Console applications are not supported.
    
## Enable Dashboard

By default, the Dashboard middleware is not launched. To enable Dashboard functionality, add the following code to your configuration:

```C#
services.AddCap(x =>
{
    // ...

    // Register Dashboard
    x.UseDashboard();
});
```

By default, you can access the Dashboard at the URL `http://localhost:xxx/cap`.

### Dashboard Configuration

* **PathMatch**

> Default: '/cap'

Change the path of the Dashboard by modifying this configuration option.

* **StatsPollingInterval**

> Default: 2000ms

Configures the polling interval for the Dashboard frontend to get the status from the /stats interface.

* **AllowAnonymousExplicit**

> Default: true

Explicitly allows anonymous access for the CAP dashboard API by passing AllowAnonymous to the ASP.NET Core global authorization filter.

* **AuthorizationPolicy**

> Default: null

Authorization policy for the Dashboard. Required if `AllowAnonymousExplicit` is false.

### Custom Authentication

From version 8.0.0, the CAP Dashboard leverages ASP.NET Core authentication mechanisms, allowing extensibility through custom authorization policies and ASP.NET Core authentication and authorization middlewares. For more details on ASP.NET Core authentication, see [the official documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0).

You can view the examples below in the `Sample.Dashboard.Auth` sample project.

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

#### Example: Open ID Connect

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
```
