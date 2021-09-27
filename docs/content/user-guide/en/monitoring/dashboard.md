# Dashboard

CAP provides a Dashboard for viewing messages, and features provided by Dashboard make it easy to view and manage messages.

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

* UseAuth 

> Default：false

Enable authentication on dashboard request.

* DefaultAuthenticationScheme 

Default scheme used for authentication. If no scheme is set, the DefaultScheme set up in AddAuthentication will be used.

* UseChallengeOnAuth

> Default：false

Enable authentication challenge on dashboard request.

* DefaultChallengeScheme 

Default scheme used for authentication challenge. If no scheme is set, the DefaultChallengeScheme set up in AddAuthentication will be used.

###  Custom authentication

From version 5.1.0, Dashboard authorization uses ASP.NET Core style by default and no longer provides custom authorization filters.

During Dashabord authentication, the value will be taken from `HttpContext.User?.Identity?.IsAuthenticated`. If it is not available, the authentication will fail and the `DefaultChallengeScheme` will be called (if configured).

You can view the usage details in the sample project `Sample.Dashboard.Auth`.

```C#
services
    .AddAuthorization()
    .AddAuthentication(options =>
    {
        options.DefaultScheme =  CookieAuthenticationDefaults.AuthenticationScheme;
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
    })
```

configuration:

```C#
services.AddCap(cap =>
{
    cap.UseDashboard(d =>
    {
        d.UseChallengeOnAuth = true;
        d.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    });
}
```
