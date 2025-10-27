# MongoDB

MongoDB is a cross-platform, document-oriented database program. Classified as a NoSQL database, MongoDB uses JSON-like documents with dynamic schema.

CAP has supported MongoDB since version 2.3. MongoDB supports ACID transactions starting from version 4.0, so CAP requires MongoDB 4.0 or higher. Additionally, MongoDB must be deployed as a cluster because ACID transactions require a replica set.

For a quick development of the MongoDB 4.0+ cluster for the development environment, you can refer to [this article](https://www.cnblogs.com/savorboard/p/mongodb-4-cluster-install.html).

## Configuration

To use MongoDB storage, you need to install the following package from NuGet:

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

The following example shows how to integrate CAP with MongoDB for local transactions:

```csharp
// NOTE: Before testing, you need to create the database and collection first.
// MongoDB cannot automatically create databases and collections within transactions,
// so you must create them separately. For example, insert a record to auto-create the collection.

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