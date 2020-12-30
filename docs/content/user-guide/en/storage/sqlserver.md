# SQL Server

SQL Server is a relational database management system developed by Microsoft. CAP supports SQL Server database. 

!!! warning "Warning"
    We currently use `Microsoft.Data.SqlClient` as the database driver, which is the future of SQL Server drivers, and we have abandoned `System.Data.SqlClient`, we suggest that you switch to.

## Configuration

To use SQL Server storage, you need to install the following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.SqlServer

```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseSqlServer(opt=>{
            //SqlServerOptions
        }); 
        // x.UseXXX ...
    });
}

```

#### SqlServerOptions

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Schema | Database schema | string | Cap
ConnectionString | Database connection string | string | 

## Publish with transaction

### ADO.NET with transaction

```csharp

private readonly ICapPublisher _capBus;

using (var connection = new SqlConnection("ConnectionString"))
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
