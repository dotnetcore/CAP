# 基本

CAP 需要使用具有持久化功能的存储介质来存储事件消息，例如通过数据库或者其他NoSql设施。

在 CAP 启动后，会向持久化介质中生成两个表，默认情况下名称为：`Cap.Published` `Cap.Received`。

## 消息存储格式

**Published** 表结构：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id | Message Id | int
Version | Message Version | string
Name | Topic Name | string
Content | Json Content | string
Added | Added Time | DateTime
ExpiresAt | Expire time | DateTime
Retries | Retry times | int
StatusName | Status Name | string

**Received** 表结构：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id | Message Id | int
Version | Message Version | string
Name | Topic Name | string
Group | Group Name | string
Content | Json Content | string
Added | Added Time | DateTime
ExpiresAt | Expire time | DateTime
Retries | Retry times | int
StatusName | Status Name | string

CAP 在进行消息发送到时候，会对原始消息对象进行一个二次包装存储到 `Content` 字段中，以下为包装 Content 的 Message 对象数据结构：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id	| CAP生成的消息编号	| string
Timestamp |	消息创建时间 |	string
Content |	内容 |	string
CallbackName |	回调的订阅者名称 | string

其中 Id 字段，CAP 采用的 MongoDB 中的 ObjectId 分布式Id生成算法生成。