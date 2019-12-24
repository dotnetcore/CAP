# 消息

使用 `ICapPublisher` 接口发送出去的数据称之为 Message (`消息`)。

## 发送 & 处理消息

你可以阅读 [quick-start](../getting-started/quick-start.md#_3) 来学习如何发送和处理消息。

## 消息调度

CAP 接收到消息之后会将消息发送到 Transport, 由 Transport 进行运输。

当你使用 `ICapPublisher` 接口发送时，CAP将会将消息调度到相应的 Transport中去，目前还不支持批量发送消息。

有关 Transports 的更多信息，可以查看 [Transports](../transports/general.md) 章节。

## 消息存储

CAP 接收到消息之后会将消息进行 Persistent（持久化）， 有关 Persistent 的更多信息，可以查看 [Persistent](../persistent/general.md) 章节。

## 消息重试

重试在整个CAP架构设计中具有重要作用，CAP 中会针对发送失败或者执行失败的消息进行重试。在整个 CAP 的设计过程中有以下几处采用的重试策略。

1、 发送重试

在消息发送过程中，当出现 Broker 宕机或者连接失败的情况亦或者出现异常的情况下，这个时候 CAP 会对发送的重试，第一次重试次数为 3，4分钟后以后每分钟重试一次，进行次数 +1，当总次数达到50次后，CAP将不对其进行重试。

你可以在 CapOptions 中设置FailedRetryCount来调整默认重试的总次数。

当失败总次数达到默认失败总次数后，就不会进行重试了，你可以在 Dashboard 中查看消息失败的原因，然后进行人工重试处理。

2、 消费重试

当 Consumer 接收到消息时，会执行消费者方法，在执行消费者方法出现异常时，会进行重试。这个重试策略和上面的 发送重试 是相同的。

## 消息数据清理

数据库消息表中具有一个 ExpiresAt 字段表示消息的过期时间，当消息发送成功或者消费成功后，CAP会将消息状态为 Successed 的 ExpiresAt 设置为 1天 后过期，会将消息状态为 Failed 的 ExpiresAt 设置为 15天 后过期。

CAP 默认情况下会每隔一个小时将消息表的数据进行清理删除，避免数据量过多导致性能的降低。清理规则为 ExpiresAt 不为空并且小于当前时间的数据。 也就是说状态为Failed的消息（正常情况他们已经被重试了 50 次），如果你15天没有人工介入处理，同样会被清理掉。
