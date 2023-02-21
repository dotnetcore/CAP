# 消息

使用 `ICapPublisher` 接口发送出去的数据称之为 Message (`消息`)。

## 发送 & 处理消息

你可以阅读 [quick-start](../getting-started/quick-start.md#_3) 来学习如何发送和处理消息。

## 补偿事务

[Compensating transaction](https://en.wikipedia.org/wiki/Compensating_transaction)

某些情况下，消费者需要返回值以告诉发布者执行结果，以便于发布者实施一些动作，通常情况下这属于补偿范围。

你可以在消费者执行的代码中通过重新发布一个新消息来通知上游，CAP 提供了一种简单的方式来做到这一点。 你可以在发送的时候指定 `callbackName` 来得到消费者的执行结果，通常这仅适用于点对点的消费。以下是一个示例。

例如，在一个电商程序中，订单初始状态为 pending，当商品数量成功扣除时将状态标记为 succeeded ，否则为 failed。

```C#
// =============  Publisher =================

_capBus.Publish("place.order.qty.deducted", 
    contentObj: new { OrderId = 1234, ProductId = 23255, Qty = 1 }, 
    callbackName: "place.order.mark.status");    

// publisher using `callbackName` to subscribe consumer result

[CapSubscribe("place.order.mark.status")]
public void MarkOrderStatus(JsonElement param)
{
    var orderId = param.GetProperty("OrderId").GetInt32();
    var isSuccess = param.GetProperty("IsSuccess").GetBoolean();
    
    if(isSuccess){
        // mark order status to succeeded
    }
    else{
       // mark order status to failed
    }
}

// =============  Consumer ===================

[CapSubscribe("place.order.qty.deducted")]
public object DeductProductQty(JsonElement param)
{
    var orderId = param.GetProperty("OrderId").GetInt32();
    var productId = param.GetProperty("ProductId").GetInt32();
    var qty = param.GetProperty("Qty").GetInt32();

    //business logic 

    return new { OrderId = orderId, IsSuccess = true };
}
```

## 异构系统集成

在 3.0+ 版本中，我们对消息结构进行了重构，我们利用了消息队列中消息协议中的 Header 来传输一些额外信息，以便于在 Body 中我们可以做到不需要修改或包装使用者的原始消息数据格式和内容进行发送。

这样的做法是合理的，它有助于在异构系统中进行更好的集成，相对于以前的版本使用者不需要知道CAP内部使用的消息结构就可以完成集成工作。

现在我们将消息划分为 Header 和 Body 来进行传输。

Body 中的数据为用户发送的原始消息内容，也就是调用 Publish 方法发送的内容，我们不进行任何包装仅仅是序列化后传递到消息队列。

在 Header 中，我们需要传递一些额外信息以便于CAP在收到消息时能够提取到关键特征进行操作。

以下是在异构系统中，需要在发消息的时候向消息的Header 中写入的内容：

 键 | 类型 | 说明
-- | --| --
cap-msg-id |  string | 消息Id， 由雪花算法生成，也可以是 guid
cap-msg-name | string | 消息名称，即 Topic 名字
cap-msg-type | string | 消息的类型, 即 typeof(T).FullName (非必须)
cap-senttime | stringg | 发送的时间 (非必须)

以 Java 系统发送 RabbitMQ 为例：

```java

Map<String, Object> headers = new HashMap<String, Object>();
headers.put("cap-msg-id",  UUID.randomUUID().toString());
headers.put("cap-msg-name", routingKey);

channel.basicPublish(exchangeName, routingKey,
             new AMQP.BasicProperties.Builder()
               .headers(headers)
               .build(),
               messageBodyBytes);
// messageBodyBytes = "发送的json".getBytes(Charset.forName("UTF-8"))
// 注意 messageBody 默认为 json 的 byte[]，如果采用其他系列化，需要在CAP侧自定义反序列化器

```

## 消息调度

CAP 接收到消息之后会将消息发送到 Transport, 由 Transport 进行运输。

当你使用 `ICapPublisher` 接口发送时，CAP将会将消息调度到相应的 Transport中去，目前还不支持批量发送消息。

有关 Transports 的更多信息，可以查看 [Transports](../transport/general.md) 章节。

## 消息存储

CAP 接收到消息之后会将消息进行 Persistent（持久化）， 有关 Persistent 的更多信息，可以查看 [Persistent](../storage/general.md) 章节。

## 消息重试

重试在整个CAP架构设计中具有重要作用，CAP 中会针对发送失败或者执行失败的消息进行重试。在整个 CAP 的设计过程中有以下几处采用的重试策略。

1、 发送重试

在消息发送过程中，当出现 Broker 宕机或者连接失败的情况亦或者出现异常的情况下，这个时候 CAP 会对发送的重试，第一次重试次数为 3，4分钟后以后每分钟重试一次，进行次数 +1，当总次数达到50次后，CAP将不对其进行重试。

你可以在 CapOptions 中设置 [FailedRetryCount](../configuration#failedretrycount) 来调整默认重试的总次数。

当失败总次数达到默认失败总次数后，就不会进行重试了，你可以在 Dashboard 中查看消息失败的原因，然后进行人工重试处理。

2、 消费重试

当 Consumer 接收到消息时，会执行消费者方法，在执行消费者方法出现异常时，会进行重试。这个重试策略和上面的 发送重试 是相同的。

## 消息数据清理

数据库消息表中具有一个 ExpiresAt 字段表示消息的过期时间，当消息发送成功或者消费成功后，CAP会将消息状态为 Successed 的 ExpiresAt 设置为 1天 后过期，会将消息状态为 Failed 的 ExpiresAt 设置为 15天 后过期（可通过 [FailedMessageExpiredAfter](../configuration#failedmessageexpiredafter) 配置)。

CAP 默认情况下会每隔**5分钟**将消息表的数据进行清理删除，避免数据量过多导致性能的降低。清理规则为 ExpiresAt 不为空并且小于当前时间的数据。 也就是说状态为Failed的消息（正常情况他们已经被重试了 50 次），如果你15天没有人工介入处理，同样会被清理掉。你可以通过 [CollectorCleaningInterval](../configuration#collectorcleaninginterval) 配置项来自定义间隔时间。

