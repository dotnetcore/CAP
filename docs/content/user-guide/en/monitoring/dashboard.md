# Dashboard

CAP 原生提供了 Dashboard 供查看消息，利用 Dashboard 提供的功能可以很方便的查看和管理消息。

## 启用 Dashboard

默认情况下，不会启动Dashboard中间件，要开启Dashboard功能你需要在配置中添加如下代码：

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

* PathMatch

默认值：'/cap'

你可以通过修改此配置项来更改Dashboard的访问路径。

* StatsPollingInterval

默认值：2000 毫秒

此配置项用来配置Dashboard 前端 获取状态接口(/stats)的轮询时间

* Authorization

此配置项用来配置访问 Dashboard 时的授权过滤器，默认过滤器允许局域网访问，当你的应用想提供外网访问时候，可以通过设置此配置来自定义认证规则。详细参看下一节

### 自定义认证

通过实现 `IDashboardAuthorizationFilter` 接口可以自定义Dashboard认证。

以下是一个示例代码，通过从url请求参数中读取 accesskey 判断是否允许访问。

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

然后在修改注册 Dashboard 时候配置此过滤对象。

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
