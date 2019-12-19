# MongoDB

MongoDB 是一个跨平台的面向文档型的数据库程序，它被归为 NOSQL 数据库，CAP 从 2.3 版本开始支持 MongoDB 作为消息存储。 

MongoDB 从 4.0 版本开始支持 ACID 事务，所以 CAP 也只支持 4.0 以上的 MongoDB，并且 MongoDB 需要部署为集群，因为 MongoDB 的 ACID 事务需要集群才可以使用。

有关开发环境如何快速搭建 MongoDB 4.0+ 集群，你可以我的参考 [这篇文章](https://www.cnblogs.com/savorboard/p/mongodb-4-cluster-install.html)。

## 配置

要使用 MongoDB 存储，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package DotNetCore.CAP.MongoDB

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于内存的配置项。

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseMongoDB(opt=>{
            //MongoDBOptions
        });
        // x.UseXXX ...
    });
}

```

#### 配置项

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
DatabaseName | 数据库名称 | string | cap 
DatabaseConnection | 数据库连接字符串 | string | mongodb://localhost:27017
ReceivedCollection | 接收消息集合名称 | string | cap.received
PublishedCollection | 发送消息集合名称 | string | cap.published

## 使用事务发布消息

下面的示例展示了如何利用 CAP 和 MongoDB 进行本地事务集成。

```csharp

//NOTE: before your test, your need to create database and collection at first
//注意：MongoDB 不能在事务中创建数据库和集合，所以你需要单独创建它们，模拟一条记录插入则会自动创建        
//var mycollection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
//mycollection.InsertOne(new BsonDocument { { "test", "test" } });

using (var session = _client.StartTransaction(_capBus, autoCommit: false))
{
    var collection = _client.GetDatabase("test").GetCollection<BsonDocument>("test.collection");
    collection.InsertOne(session, new BsonDocument { { "hello", "world" } });

    _capBus.Publish("sample.rabbitmq.mongodb", DateTime.Now);

    session.CommitTransaction();
}
     
```