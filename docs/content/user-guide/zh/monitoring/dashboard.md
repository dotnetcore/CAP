# Dashboard

CAP 原生提供了 Dashboard 供查看消息，利用 Dashboard 提供的功能可以很方便的查看和管理消息。

## 启用 Dashboard

首先，你需要安装Dashboard的 NuGet 包。

```powershell
PM> Install-Package DotNetCore.CAP.Dashboard
```

然后，在配置中添加如下代码：

```C#
services.AddCap(x =>
{
    //...

    // Register Dashboard
    x.UseDashboard();
});
```

默认情况下，你可以访问 `http://localhost:xxx/cap` 这个地址打开Dashboard。 

### Dashboard 配置项

* PathBase

默认值：N/A

当位于代理后时，通过配置此参数可以指定代理请求前缀。

* PathMatch

默认值：'/cap'

你可以通过修改此配置项来更改Dashboard的访问路径。

* StatsPollingInterval

默认值：2000 毫秒

此配置项用来配置Dashboard 前端 获取状态接口(/stats)的轮询时间

* UseAuth 

默认值：false

指定是否开启授权

* DefaultAuthenticationScheme 

授权默认使用的 Scheme 

* UseChallengeOnAuth

默认值：false

授权是否启用 Challenge

* DefaultChallengeScheme 

Challenge 默认使用的 Scheme


### 自定义认证
 
自 5.1.0 开始，CAP Dashboard 授权默认使用 ASP.NET Core 的方式，不再提供自定义授权过滤器。

在 Dashabord 认证时，会从 HttpContext.User?.Identity?.IsAuthenticated 中取值，如果取不到则认证失败，并调用 Challenge Scheme(如进行配置)。

你可以在 Sample.Dashboard.Auth 这个示例项目中查看使用细节。

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

配置

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
