# Message

The data sent by using the `ICapPublisher` interface is called `Message`.

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