# Dashboard

CAP 原生提供了 Dashboard 供查看消息，利用 Dashboard 提供的功能可以很方便的查看和管理消息。

!!! WARNING "使用限制"
    Dashboard 只支持在 ASP.NET Core 中使用，不支持控制台应用(Console App)

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

* AllowAnonymousExplicit

> Default: true

显式允许对 CAP 仪表板 API 进行匿名访问，当启用ASP.NET Core 全局授权筛选器请启用 AllowAnonymous。

* AuthorizationPolicy

> Default: null.

Dashboard 的授权策略。 需设置 `AllowAnonymousExplicit`为 false。

###  自定义认证

从版本 8.0.0 开始，CAP 仪表板利用 ASP.NET Core 身份验证机制，允许通过自定义授权策略和 ASP.NET Core 身份验证和授权中间件进行扩展，以授权仪表板访问。 有关 ASP.NET Core 身份验证内部结构的更多详细信息，请查看[官方文档](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0).

您可以在示例项目 `Sample.Dashboard.Auth`中查看示例代码。

#### Example: 匿名访问

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

### 自定义认证

从 8.0.0 版开始，CAP 控制面板利用 ASP.NET Core 身份验证机制，允许通过自定义授权策略和 ASP.NET Core 身份验证与授权中间件进行扩展。有关 ASP.NET Core 身份验证内部机制的更多详情，请查阅 [官方文档](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0)。

您可以在示例项目 `Sample.Dashboard.Auth` 中查看以下示例。

#### 例子：Anonymous Access 匿名访问

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

#### 例子：使用 Open Id

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

#### 例子：自定义 Authentication Scheme

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
