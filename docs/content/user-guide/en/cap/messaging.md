# Message

The data sent using the `ICapPublisher` interface is called a `Message`.

!!! WARNING "TimeoutException thrown in consumer using HTTPClient"
    By default, if the consumer throws an `OperationCanceledException` (including `TaskCanceledException`), it is considered normal user behavior, and the exception is ignored. However, if you use `HttpClient` in the consumer method and configure a request timeout, you may need to handle exceptions separately and re-throw non-`OperationCanceledException` exceptions due to a [design issue](https://github.com/dotnet/runtime/issues/21965) in `HttpClient`. Refer to issue #1368 for more details.

## Compensating Transaction

Wiki: [Compensating Transaction](https://en.wikipedia.org/wiki/Compensating_transaction)

In some cases, consumers need to return an execution result to notify the publisher, allowing the publisher to perform compensation actions. This process is called message compensation.

Typically, you can notify the upstream system by publishing a new message in the consumer code. CAP simplifies this by allowing you to specify the `callbackName` parameter when publishing a message. This feature is generally applicable to point-to-point consumption. Here is an example:

For instance, in an e-commerce application, an order's initial status is "pending." The status is updated to "succeeded" when the product quantity is successfully deducted; otherwise, it is marked as "failed."

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

### Controlling Callback Response

You can inject the `CapHeader` parameter in the subscription method using the `[FromCap]` attribute and use its methods to add extra headers to the callback context or terminate the callback.

Example:

```cs
[CapSubscribe("place.order.qty.deducted")]
public object DeductProductQty(JsonElement param, [FromCap] CapHeader header)
{
    var orderId = param.GetProperty("OrderId").GetInt32();
    var productId = param.GetProperty("ProductId").GetInt32();
    var qty = param.GetProperty("Qty").GetInt32();

    // Add additional headers to the response message
    header.AddResponseHeader("some-message-info", "this is the test");
    // Or add a callback to the response
    header.AddResponseHeader(DotNetCore.CAP.Messages.Headers.CallbackName, "place.order.qty.deducted-callback");

    // If you no longer want to follow the sender's specified callback and want to modify it, use the RewriteCallback method.
    header.RewriteCallback("new-callback-name");

    // If you want to terminate/stop, or no longer respond to the sender, call RemoveCallback to remove the callback.
    header.RemoveCallback();

    return new { OrderId = orderId, IsSuccess = true };
}
```

## Heterogeneous system integration

In version 3.0+, we reconstructed the message structure. We used the Header in the message protocol in the message queue to transmit some additional information, so that we can do it in the Body without modifying or packaging the user’s original The message data format and content are sent.

This approach facilitates better integration with heterogeneous systems. Compared to previous versions, users no longer need to understand the internal message structure used by CAP to complete integration tasks.

Now we divide the message into Header and Body for transmission.

The data in the body is the content of the original message sent by the user, that is, the content sent by calling the Publish method. We do not perform any packaging, but send it to the message queue after serialization.

In the Header, we need to pass some additional information so that the CAP can extract the key features for operation when the message is received.

The following is the content that needs to be written into the header of the message when sending a message in a heterogeneous system:

 | Key           | DataType | Description                                                    |
 | ------------- | -------- | -------------------------------------------------------------- |
 | cap-msg-id    | long     | Message Id, Generated by snowflake algorithm                   |
 | cap-msg-name  | string   | The name of the message                                        |
 | cap-msg-type  | string   | The type of message, `typeof(T).FullName`(not required)        |
 | cap-senttime  | string   | sending time (not required)                                    |
 | cap-kafka-key | string   | Partitioning by Kafka Key                                      |

### Custom headers

To consume messages sent without CAP headers, Azure Service Bus, Kafka, and RabbitMQ consumers can inject a minimal set of headers using the `CustomHeadersBuilder` property as shown below (RabbitMQ example):
```C#
container.AddCap(x =>
{
    x.UseRabbitMQ(z =>
    {
        z.ExchangeName = "TestExchange";
        z.CustomHeadersBuilder = (msg, sp) =>
        [
            new(DotNetCore.CAP.Messages.Headers.MessageId, sp.GetRequiredService<ISnowflakeId>().NextId().ToString()),
            new(DotNetCore.CAP.Messages.Headers.MessageName, msg.RoutingKey)
        ];
    });
});
```

After adding `cap-msg-id` and `cap-msg-name`, CAP consumers can receive messages sent directly from external systems, such as the RabbitMQ management tool when using RabbitMQ as a transport.

To publish messages with CAP headers:

```C#
var headers = new Dictionary<string, string?>()
{
    { "cap-kafka-key", request.OrderId }
};
_publisher.Publish<OrderRequest>("OrderRequest", request, headers);
```

## Scheduling

After CAP receives a message, it sends the message to Transport (RabbitMQ, Kafka...), which handles the transportation.
 
When you send a message using the `ICapPublisher` interface, CAP dispatches it to the corresponding Transport. Currently, bulk messaging is not supported.

For more information on transports, see the [Transports](../transport/general.md) section.

## Storage 

CAP stores messages after receiving them. For more information on storage, see the [Storage](../storage/general.md) section.

## Retry

Retrying is a crucial aspect of the CAP architecture. CAP retries messages that fail to send or consume, employing several retry strategies throughout its design.

### Send Retry

When the broker crashes, connection fails, or an abnormality occurs during message sending, CAP retries the send. It performs 3 immediate retries, then after 4 minutes (FallbackWindowLookbackSeconds), it retries every minute with a +1 increment. When the total number of retries reaches 50, CAP stops retrying.

You can adjust the total number of retries by setting [FailedRetryCount](configuration.md#failedretrycount) in CapOptions or use [FailedThresholdCallback](configuration.md#failedthresholdcallback) to receive notifications when the maximum retry count is reached.

Retries will stop when the maximum is reached. You can see the failure reason in Dashboard and choose whether to manually retry.

### Consumption Retry

When the Consumer receives a message, the consumer method is executed and will retry if an exception occurs. This retry strategy is the same as the send retry.

Version 7.1.0 introduced database-based distributed locks to handle concurrent database fetches during retry operations across multiple instances. You need to explicitly configure the `UseStorageLock` option to true.

Whether sending or consumption fails, the exception message is stored in the cap-exception field within the message header. You can find it in the Content field's JSON in the database table.

## Data Cleanup

The database message table has an `ExpiresAt` field indicating the message expiration time. When a message is sent successfully, its status changes to `Successed`, and `ExpiresAt` is set to **1 day** later. 

When consumption fails, the message status changes to `Failed` and `ExpiresAt` is set to **15 days** later (you can customize this using the [FailedMessageExpiredAfter](configuration.md#failedmessageexpiredafter) configuration option).

By default, messages in the table are deleted every **5 minutes** to prevent performance degradation from excessive data. The cleanup process is performed when the `ExpiresAt` field is not empty and is less than the current time. 

That is, messages with `Failed` status (by default, they have been retried 50 times) will also be cleaned up after **15 days** if you do not manually intervene.

You can customize the cleanup interval time using the [CollectorCleaningInterval](configuration.md#collectorcleaninginterval) configuration option.
