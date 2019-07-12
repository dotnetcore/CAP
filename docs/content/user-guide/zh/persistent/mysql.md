# MySQL

MySQL 是一个开源的关系型数据库，你可以使用 MySQL 来作为 CAP 消息的持久化。

## 配置

要使用 MySQL 存储，你需要从 NuGet 安装以下扩展包：

```shell
Install-Package DotNetCore.CAP.MySql
```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于内存的配置项。

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
TableNamePrefix | Cap表名前缀 | string | cap 
ConnectionString | 数据库连接字符串 | string | null

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