# Postgre SQL


### PostgreSql Configs

Note that if you are using EntityFramewrok, you do not use this configuration item.

CAP uses PostgreSql configuration functions for CapOptions extensions, so the configuration usage for PostgreSql is as follows:

```c#
services.AddCap(capOptions => {
    capOptions.UsePostgreSql(postgreOptions => {
       // postgreOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
Schema | Cap table name prefix | string | cap
ConnectionString | Database connection string | string | null