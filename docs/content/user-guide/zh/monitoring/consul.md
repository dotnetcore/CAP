# Consul

[Consul](https://www.consul.io/) 是一个分布式服务网格，用于跨任何运行时平台和公共或私有云连接，保护和配置服务。

## Dashboard 中的 Consul 配置

CAP的 Dashboard 使用 Consul 作为服务发现来显示其他节点的数据，然后你就在任意节点的 Dashboard 中切换到 Servers 页面看到其他的节点。

![](https://camo.githubusercontent.com/54c00c6ae65ce1d7b9109ed8cbcdca703a050c47/687474703a2f2f696d61676573323031372e636e626c6f67732e636f6d2f626c6f672f3235303431372f3230313731302f3235303431372d32303137313030343232313030313838302d313136323931383336322e706e67)

通过点击 Switch 按钮来切换到其他的节点看到其他节点的数据，而不必访问很多地址来分别查看。
 
以下是一个配置示例, 你需要在每个节点分别配置：

```C#
services.AddCap(x =>
{
    x.UseMySql(Configuration.GetValue<string>("ConnectionString"));
    x.UseRabbitMQ("localhost");
    x.UseDashboard();
    x.UseDiscovery(_ =>
    {
        _.DiscoveryServerHostName = "localhost";
        _.DiscoveryServerPort = 8500;
        _.CurrentNodeHostName = Configuration.GetValue<string>("ASPNETCORE_HOSTNAME");
        _.CurrentNodePort = Configuration.GetValue<int>("ASPNETCORE_PORT");
        _.NodeId = Configuration.GetValue<string>("NodeId");
        _.NodeName = Configuration.GetValue<string>("NodeName");
    });
});
```

Consul 1.6.2:

```
consul agent -dev
```

Windows 10, ASP.NET Core 3.1:

```sh
set ASPNETCORE_HOSTNAME=localhost&& set ASPNETCORE_PORT=5001&& dotnet run --urls=http://localhost:5001 NodeId=1 NodeName=CAP-1 ConnectionString="Server=localhost;Database=aaa;UserId=xxx;Password=xxx;"
set ASPNETCORE_HOSTNAME=localhost&& set ASPNETCORE_PORT=5002&& dotnet run --urls=http://localhost:5002 NodeId=2 NodeName=CAP-2 ConnectionString="Server=localhost;Database=bbb;UserId=xxx;Password=xxx;"
```