# PostgreSQL

PostgreSQL is an open-source relational database management system. CAP fully supports PostgreSQL. 

## Configuration

To use PostgreSQL storage, you need to install the following package from NuGet:

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

NAME | DESCRIPTION                | TYPE                 | DEFAULT
:---|:---------------------------|----------------------|:---
Schema | Database schema            | string               | cap 
ConnectionString | Database connection string | string               |
DataSource | [Data source](https://www.npgsql.org/doc/basic-usage.html#data-source) | [NpgsqlDataSource](https://www.npgsql.org/doc/api/Npgsql.NpgsqlDataSource.html) |

## Publish with transaction

### ADO.NET with Transaction

```csharp
private readonly ICapPublisher _capBus;

using (var connection = new NpgsqlConnection("ConnectionString"))
{
    using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
    {
        // Your business code
        connection.Execute("insert into test(name) values('test')", 
            transaction: (IDbTransaction)transaction.DbTransaction);
        
        _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

        transaction.Commit();
    }
}
```

### Entity Framework with Transaction

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