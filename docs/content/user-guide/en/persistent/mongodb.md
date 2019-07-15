# MongoDB

MongoDB is a cross-platform document-oriented database program. Classified as a NoSQL database program, MongoDB uses JSON-like documents with schema.

CAP has supported MongoDB as persistent since version 2.3 .

MongoDB supports ACID transactions since version 4.0, so CAP only supports MongoDB above 4.0, and MongoDB needs to be deployed as a cluster, because MongoDB's ACID transaction requires a cluster to be used.

For a quick development of the MongoDB 4.0+ cluster for the development environment, you can refer to [this article](https://www.cnblogs.com/savorboard/p/mongodb-4-cluster-install.html).

## Configuration

To use MongoDB storage, you need to install the following extensions from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.MongoDB

```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`.

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

#### MongoDB Options

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
DatabaseName | Database name | string | cap 
DatabaseConnection | Database connection string | string | mongodb://localhost:27017
ReceivedCollection | Database received message collection name | string | cap.received
PublishedCollection | Database published message collection name | string | cap.published

## Publish with transaction

The following example shows how to leverage CAP and MongoDB for local transaction integration.


```csharp

//NOTE: Before your test, your need to create database and collection at first.
//      Mongo can't create databases and collections in transactions automatic, 
//      so you need to create them separately, simulating a record insert 
//      will automatically create.

// var mycollection = _client.GetDatabase("test")
//          .GetCollection<BsonDocument>("test.collection");
// mycollection.InsertOne(new BsonDocument { { "test", "test" } });

using (var session = _client.StartTransaction(_capBus, autoCommit: false))
{
    var collection = _client.GetDatabase("test")
            .GetCollection<BsonDocument>("test.collection");

    collection.InsertOne(session, new BsonDocument { { "hello", "world" } });

    _capBus.Publish("sample.rabbitmq.mongodb", DateTime.Now);

    session.CommitTransaction();
}
     
```