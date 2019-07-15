# Dashboard

The CAP provides a Dashboard for viewing messages, and the features provided by Dashboard make it easy to view and manage messages.

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

You can change the path of the Dashboard by modifying this configuration item.

* StatsPollingInterval

> Default: 2000ms

This configuration item is used to configure the Dashboard front end to get the polling time of the status interface (/stats).

* Authorization

This configuration item is used to configure the authorization filter when accessing the Dashboard. The default filter allows LAN access. When your application wants to provide external network access, you can customize the authentication rules by setting this configuration. See the next section for details.

### Custom authentication

Dashboard authentication can be customized by implementing the `IDashboardAuthorizationFilter` interface.

The following is a sample code that determines if access is allowed by reading the accesskey from the url request parameter.

```C#
public class TestAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if(context.Request.GetQuery("accesskey")=="xxxxxx"){
            return true;
        }
        return false;
    }
}
```

Then configure this filter when registration Dashboard.

```C#
services.AddCap(x =>
{
    //...

    // Register Dashboard
    x.UseDashboard(opt => {
        opt.Authorization = new[] {new TestAuthorizationFilter()};
    });
});
```
