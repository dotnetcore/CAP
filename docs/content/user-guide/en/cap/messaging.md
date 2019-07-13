# Message

The data sent by using the `ICapPublisher` interface is called `Message`.

## Scheduling

After the CAP receives the message, it sends the message to Transport, which is transported by transport.
 
When you send using the `ICapPublisher` interface, the CAP will dispatch the message to the corresponding Transport. Currently, bulk messaging is not supported.

For more information on transports, see the [Transports](../transports/general.md) section.

## Persistent 

The CAP will storage after receiving the message. For more information on storage, see the [Persistent](../persistent/general.md) section.

## Retry

Retrying plays an important role in the overall CAP architecture design, and CAPs retry for messages that fail to send or fail to execute. There are several retry strategies used throughout the CAP design process.

### Send retry

During the message sending process, when the broker crashes or the connection fails or an abnormality occurs, the CAP will retry the sending. Retry 3 times for the first time, retry every minute after 4 minutes, and +1 retries. When the total number of times reaches 50, the CAP will stop retrying.

You can adjust the total number of default retries by setting `FailedRetryCount` in CapOptions.

It will stop when the maximum number of times is reached. You can see the reason for the failure in Dashboard and choose whether to manually retry.

### Consumption retry

The consumer method is executed when the Consumer receives the message and will retry when an exception occurs. This retry strategy is the same as the send retry.

## Data Cleanup

There is an `ExpiresAt` field in the database message table indicating the expiration time of the message. When the message is sent successfully, the status will be changed to `Successed`, and `ExpiresAt` will be set to **1 hour** later. 

Consuming failure will change the message status to `Failed` and `ExpiresAt` will be set to **15 days** later.

By default, the data of the message table is deleted **every hour** to avoid performance degradation caused by too much data. The cleanup strategy is `ExpiresAt` is not empty and is less than the current time. 

That is to say, the message with the status Failed (normally they have been retried 50 times), if you do not have manual intervention for 15 days, it will **also be** cleaned up.