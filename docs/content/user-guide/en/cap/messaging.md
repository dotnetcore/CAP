# Message

The data sent by using the `ICapPublisher` interface is called `Message`.

## Compensating transaction

Wiki :
[Compensating transaction](https://en.wikipedia.org/wiki/Compensating_transaction)

In some cases, consumers need to return the execution value to tell the publisher, so that the publisher can implement some compensation actions, usually we called message compensation.

Usually you can notify the upstream by republishing a new message in the consumer code. CAP provides a simple way to do this. You can specify `callbackName` parameter when publishing message, usually this only applies to point-to-point consumption. The following is an example.

For example, in an e-commerce application, the initial status of the order is pending, and the status is marked as succeeded when the product quantity is successfully deducted, otherwise it is failed.

```C#
// =============  Publisher =================

_capBus.Publish("place.order.qty.deducted", new { OrderId = 1234, ProductId = 23255, Qty = 1 }, "place.order.mark.status");    

// publisher using `callbackName` to subscribe consumer result

[CapSubscribe("place.order.mark.status")]
public void MarkOrderStatus(JToken param)
{
    var orderId = param.Value<int>("OrderId");
    var isSuccess = param.Value<bool>("IsSuccess");
    
    if(isSuccess)
       //mark order status to succeeded
    else
       //mark order status to failed
}

// =============  Consumer ===================

[CapSubscribe("place.order.qty.deducted")]
public object DeductProductQty(JToken param)
{
    var orderId = param.Value<int>("OrderId");
    var productId = param.Value<int>("ProductId");
    var qty = param.Value<int>("Qty");

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

## Scheduling

After CAP receives a message, it sends the message to Transport(RabitMq, Kafka...), which is transported by transport.
 
When you send message using the `ICapPublisher` interface, CAP will dispatch message to the corresponding Transport. Currently, bulk messaging is not supported.

For more information on transports, see [Transports](../transport/general.md) section.

## Storage 

CAP will store the message after receiving it. For more information on storage, see the [Storage](../storage/general.md) section.

## Retry

Retrying plays an important role in the overall CAP architecture design, CAP retry messages that fail to send or fail to execute. There are several retry strategies used throughout the CAP design process.

### Send retry

During the message sending process, when the broker crashes or the connection fails or an abnormality occurs, CAP will retry the sending. Retry 3 times for the first time, retry every minute after 4 minutes, and +1 retry. When the total number of retries reaches 50,CAP will stop retrying.

You can adjust the total number of retries by setting `FailedRetryCount` in CapOptions.

It will stop when the maximum number of times is reached. You can see the reason for the failure in Dashboard and choose whether to manually retry.

### Consumption retry

The consumer method is executed when the Consumer receives the message and will retry when an exception occurs. This retry strategy is the same as the send retry.

## Data Cleanup

There is an `ExpiresAt` field in the database message table indicating the expiration time of the message. When the message is sent successfully, status will be changed to `Successed`, and `ExpiresAt` will be set to **1 day** later. 

Consuming failure will change the message status to `Failed` and `ExpiresAt` will be set to **15 days** later.

By default, the data of the message in the table is deleted **every hour** to avoid performance degradation caused by too much data. The cleanup strategy `ExpiresAt` is performed when field is not empty and is less than the current time. 

That is to say, the message with the status Failed (by default they have been retried 50 times), if you do not have manual intervention for 15 days, it will **also be** cleaned up.