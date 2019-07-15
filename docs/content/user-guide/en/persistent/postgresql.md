# Postgre SQL

PostgreSQL is an open-source relational database management system. CAP has supported PostgreSQL as persistent. 

## Configuration

To use PostgreSQL storage, you need to install the following extensions from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.PostgreSql

```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UsePostgreSql(opt=>{
            //PostgreSqlOptions
        }); 
        // x.UseXXX ...
    });
}

```

#### PostgreSqlOptions

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Schema | Database schema | string | cap 
ConnectionString | Database connection string | string | 

## Publish with transaction

### ADO.NET with transaction

```csharp

private readonly ICapPublisher _capBus;

using (var connection = new NpgsqlConnection("ConnectionString"))
{
    using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
    {
        //your business code
        connection.Execute("insert into test(name) values('test')", 
            transaction: (IDbTransaction)transaction.DbTransaction);
        
        _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

        transaction.Commit();
    }
}
```

### EntityFramework with transaction

```csharp

private readonly ICapPublisher _capBus;

using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: false))
{
    dbContext.Persons.Add(new Person() { Name = "ef.transaction" });
    
    _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

    dbContext.SaveChanges();
    trans.Commit();
}

```