# Idempotence

Idempotence (which you may read a formal definition of on [Wikipedia](https://en.wikipedia.org/wiki/Idempotence)), when we are talking about messaging, is when a message redelivery can be handled without ending up in an unintended state.

Since we always run the risk of processing the same message twice, it is a good idea to think a little bit about idempotence from time to time.

## CAP 中的幂等

CAP 没有实现幂等有以下几个原因 ： 以下都是接收端

1、消息写入成功了，但是此时执行Consumer方法失败了

执行Consumer方法失败的原因有非常多，我如果不知道具体的场景盲目进行重试或者不进行重试都是不正确的选择。
举个例子：假如消费者为扣款服务，如果是执行扣款成功了，但是在写扣款日志的时候失败了，此时CAP会判断为消费者执行失败，进行重试。如果客户端自己没有保证幂等性，框架对其进行重试，这里势必会造成多次扣款出现严重后果。

2、执行Consumer方法成功了，但是又收到了同样的消息

此处场景也是可能存在的，假如开始的时候Consumer已经执行成功了，但是由于某种原因如 Broker 宕机恢复等，又收到了相同的消息，CAP 在收到Broker消息后会认为这个是一个新的消息，会对 Consumer再次执行，由于是新消息，此时 CAP 也是无法做到幂等的。

3、目前的数据存储模式无法做到幂等

由于CAP存消息的表对于成功消费的消息会于1个小时后删除，所以如果对于一些历史性消息无法做到幂等操作。 历史性指的是，假如 Broker由于某种原因维护了或者是人工处理的一些消息。

4、业界做法

许多基于事件驱动的框架都是要求 用户 来保证幂等性操作的，比如 ENode, RocketMQ 等等...

从实现的角度来说，CAP可以做一些比较不严格的幂等，但是严格的幂等无法做到的。