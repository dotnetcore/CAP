# MySQL

MySQL is an open-source relational database management system. CAP has supported MySQL as persistent. 

## Configuration

To use MySQL storage, you need to install the following extensions from NuGet:
 
```powershell
PM> Install-Package DotNetCore.CAP.MySql

```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseMySql(opt=>{
            //MySqlOptions
        });
        // x.UseXXX ...
    });
}

```

#### MySqlOptions

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
TableNamePrefix | CAP table name prefix | string | cap 
ConnectionString | Database connection string | string | null

## Publish with transaction

### ADO.NET with transaction

```csharp

private readonly ICapPublisher _capBus;

using (var connection = new MySqlConnection(AppDbContext.ConnectionString))
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