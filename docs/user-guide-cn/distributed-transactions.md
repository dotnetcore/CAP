针对于分布式事务的处理，CAP 采用的是“异步确保”这种方案。

### 异步确保

异步确保这种方案又叫做本地消息表，这是一种经典的方案，方案最初来源于 eBay，参考资料见段末链接。这种方案目前也是企业中使用最多的方案之一。

相对于 TCC 或者 2PC/3PC 来说，这个方案对于分布式事务来说是最简单的，而且它是去中心化的。在TCC 或者 2PC 的方案中，必须具有事务协调器来处理每个不同服务之间的状态，而此种方案不需要事务协调器。
另外 2PC/TCC 这种方案如果服务依赖过多，会带来管理复杂性增加和稳定性风险增大的问题。试想如果我们强依赖 10 个服务，9 个都执行成功了，最后一个执行失败了，那么是不是前面 9 个都要回滚掉？这个成本还是非常高的。

但是，并不是说 2PC 或者 TCC 这种方案不好，因为每一种方案都有其相对优势的使用场景和优缺点，这里就不做过多介绍了。

> 中文：[http://www.cnblogs.com/savorboard/p/base-an-acid-alternative.html](http://www.cnblogs.com/savorboard/p/base-an-acid-alternative.html)  
> 英文：[http://queue.acm.org/detail.cfm?id=1394128](http://queue.acm.org/detail.cfm?id=1394128)
