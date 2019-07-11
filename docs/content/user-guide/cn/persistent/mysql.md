# MySQL


### MySql Options

注意，如果你使用的是 EntityFramewrok，你用不到此配置项。

CAP 采用的是针对 CapOptions 进行扩展来实现 MySql 的配置功能，所以针对 MySql 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UseMySql(mysqlOptions => {
       // mysqlOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
TableNamePrefix | Cap表名前缀 | string | cap 
ConnectionString | 数据库连接字符串 | string | null