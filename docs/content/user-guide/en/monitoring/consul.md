# Consul

[Consul](https://www.consul.io/) is a distributed service mesh to connect, secure, and configure services across any runtime platform and public or private cloud.

## Consul Configuration for dashboard

CAP's Dashboard uses Consul as a service discovery to get the data of other nodes, and you can switch to the Servers page to see other nodes.

![](https://camo.githubusercontent.com/54c00c6ae65ce1d7b9109ed8cbcdca703a050c47/687474703a2f2f696d61676573323031372e636e626c6f67732e636f6d2f626c6f672f3235303431372f3230313731302f3235303431372d32303137313030343232313030313838302d313136323931383336322e706e67)

Click the `Switch` button to switch to the target node, CAP will use a proxy to get the data of the node you switched to.

The following is a configuration example, you need to configure them on each node.

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