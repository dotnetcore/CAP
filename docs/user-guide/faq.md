# FAQ

## Any IM group(e.g Tencent QQ group) to learn and chat about CAP?

None for that. Better than wasting much time in IM group, I hope developers could be capable of independent thinking more, and solve problems yourselves with referenced documents, even create issues or send emails when errors are remaining present.

##  Does it require certain different databases, one each for productor and resumer in CAP?

Not requird differences necessary, a given advice is that using a special database for each program.

Otherwise, look at Q&A below.

##  How to use the same database for different programs?

defining a prefix name of table in `ConfigureServices` method。
 
codes exsample：

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddCap(x =>
    {
        x.UseKafka("");
        x.UseMySql(opt =>
        {
            opt.ConnectionString = "connection string";
            opt.TableNamePrefix = "appone"; // different table name prefix here
        });
    });
}
```

!!!NOTE
    Different prefixed names cause SLB to no effect.

##  If don't care about message missing, can message productor exist without any database, for the reason of sending message only.

Not yet.

The purpose of CAP is that ensure consistency principle right in microservice or SOA architechtrues. The solution is based on ACID features of database, there is no sense about a single client wapper of message queue without database.
