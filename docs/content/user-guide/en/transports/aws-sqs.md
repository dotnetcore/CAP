# Amazon SQS

AWS SQS is a fully managed message queuing service that enables you to decouple and scale microservices, distributed systems, and serverless applications.

AWS SNS is a highly available, durable, secure, fully managed pub/sub messaging service that enables you to decouple microservices, distributed systems, and serverless applications.

## How CAP uses AWS SNS and SQS

### SNS

Because CAP works based on the topic pattern, it needs to use AWS SNS, which simplifies the publish and subscribe architecture of messages.

When CAP startups, all subscription names will be registered as SNS topics, and you will see a list of all registered topics in the management console.

SNS does not support use of symbols such as `.` `:` as the name of the topic, so we replaced it. We replaced `.` with `-` and `:` with `_`

!!! note "Precautions"
    Amazon SNS currently allows maximum size of published messages to be 256KB

For example, you have the following two subscriber methods in your current project

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
After CAP startups, you will see in SNS management console:

![img](/img/aws-sns-demo.png)

### SQS

For each consumer group, CAP will create a corresponding SQS queue, the name of the queue is the name of the `DefaultGroup` in the configuration options, and the queue type is Standard.

The SQS queue will subscribe to Topic in SNS, as shown below:

![img](/img/aws-sns-demo.png)

!!! warning "Precautions"
    Due to the limitation of AWS SNS, when you remove the subscription method, CAP will not delete topics or queues on AWS SNS or SQS, you need to delete them manually.


## Configuration

To use AWS SQS as the transport, you need to install the packages from NuGet:

```shell

Install-Package DotNetCore.CAP.AmazonSQS

```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`:

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