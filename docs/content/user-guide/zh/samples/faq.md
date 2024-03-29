# FAQ

!!! faq "有没有学习和讨论 CAP 的即时通讯群组（例如腾讯 QQ 群）？"

回答： 暂时没有。与其浪费大量时间在即时通讯群组里，我更希望开发者能够培养独立思考能力，并通过查阅文档自行解决问题，甚至可以在遇到错误时创建issue或发送电子邮件。

!!! faq "CAP 是否需要为生产者和消费者分别使用不同的数据库？"

回答：没有必要使用完全不同的数据库，推荐为每个程序使用一个专用数据库。

否则，请参阅下面的问答部分。

!!! faq "如何使用相同的数据库用于不同的应用程序？"

回答： 在 ConfigureServices 方法中定义表名前缀。

代码示例：

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddCap(x =>
    {
        x.UseKafka("");
        x.UseMySql(opt =>
        {
            opt.ConnectionString = "connection string";
            opt.TableNamePrefix = "appone"; // different table name prefix here
        });
    });
}
```

!!! faq "CAP 能否不使用数据库作为事件存储？我只是想发送消息"

回答： 完全不用是不可能的，你可以使用 InMemoryStorage 。

CAP 的目的是在微服务或 SOA 架构中确保一致性原则。该解决方案基于数据库的 ACID 特性，如果没有数据库，单纯的消息队列消息传递是没有意义的。

!!! faq "如果消费者出现异常，能否回滚生产者执行的数据库语句？"

回答： 无法回滚，CAP 是最终一致性解决方案。

可以想象您的场景是调用第三方支付。如果您正在进行第三方支付操作，在成功调用支付宝的接口后，您的代码出现错误，支付宝会回滚吗？如果不回滚，您该怎么办？CAP 的情况与此类似。