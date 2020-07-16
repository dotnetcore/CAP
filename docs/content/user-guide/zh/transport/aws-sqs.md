# Amazon SQS

AWS SQS 是一种完全托管的消息队列服务，可让您分离和扩展微服务、分布式系统和无服务器应用程序。

AWS SNS 是一种高度可用、持久、安全、完全托管的发布/订阅消息收发服务，可以轻松分离微服务、分布式系统和无服务器应用程序。

## CAP 如何使用 AWS SNS & SQS

### SNS

由于 CAP 是基于 Topic 模式工作的，所以需要使用到 AWS SNS，SNS 简化了消息的发布订阅架构。

在 CAP 启动时会将所有的订阅名称注册为 SNS 的 Topic，你将会在管理控制台中看到所有已经注册的 Topic 列表。 

由于 SNS 不支持使用 `.` `:` 等符号作为 Topic 的名称，所以我们进行了替换，我们将 `.` 替换为了 `-`，将 `:` 替换为了 `_`

!!! note "注意事项"
    Amazon SNS 当前允许发布的消息最大大小为 256KB

举例，你的当前项目中有以下两个订阅者方法

```C#
[CapSubscribe("sample.sns.foo")]
public void TestFoo(DateTime value)
{
}

[CapSubscribe("sample.sns.bar")]
public void TestBar(DateTime value)
{
}
```

在 CAP 启动后，在 AWS SNS 中你将看到

![img](/img/aws-sns-demo.png)

### SQS

针对每个消费者组，CAP 将创建一个与之对应的 SQS 队列，队列的名称为配置项中 DefaultGroup 的名称，类型为 Standard Queue 。

该 SQS 队列将订阅 SNS 中的 Topic ，如下图：

![img](/img/aws-sns-demo.png)

!!! warning "注意事项"
    由于 AWS SNS 的限制，当你减少订阅方法时，我们不会主动删除 AWS SNS 或者 SQS 上的相关 Topic 或 Queue，你需要手动删除他们。


## 配置

要使用 AWS SQS 作为消息传输器，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package DotNetCore.CAP.AmazonSQS

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于 RabbitMQ 的配置项。

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseAmazonSQS(opt=>
        {
            //AmazonSQSOptions
        });
        // x.UseXXX ...
    });
}

```

#### AmazonSQS Options

CAP 直接对外提供的 AmazonSQSOptions 配置参数如下：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Region | AWS 所处的区域 | Amazon.RegionEndpoint | 
Credentials | AWS AK SK信息 | Amazon.Runtime.AWSCredentials | 

如果你的项目运行在 AWS EC2 中，则不需要设置 Credentials，直接对 EC2 应用 IAM 策略即可。

Credentials 需要具有新增和订阅 SNS Topic，SQS Queue 等权限。