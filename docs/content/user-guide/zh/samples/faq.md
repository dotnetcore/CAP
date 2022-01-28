# FAQ

!!! faq "Any IM group(e.g Tencent QQ group) to learn and chat about CAP?"

    None of that. Better than wasting much time in IM group, I hope developers could be capable of independent thinking more, and solve problems yourselves with referenced documents, even create issues or send emails when errors are remaining present.

!!! faq "Does it require different databases, one each for producer and consumer in CAP?"

    No difference necessary, a recommendation is to use a dedicated database for each program.

    Otherwise, look at Q&A below.

!!! faq "How to use the same database for different applications?"
    
    Define a table prefix name in `ConfigureServices` method.
    
    Code example：

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

!!! faq "Can CAP not use the database as event storage? I just want to send the message"

    Not yet.

    The purpose of CAP is that ensure consistency principle right in microservice or SOA architectures. The solution is based on ACID features of database, there is no sense about a single client wapper of message queue without database.

!!! faq "If the consumer is abnormal, can I roll back the database executed sql that the producer has executed?"

    Can't roll back, CAP is the ultimate consistency solution.

    You can imagine your scenario is to call a third party payment. If you are doing a third-party payment operation, after calling Alipay's interface successfully, and your own code is wrong, will Alipay roll back? If you don't roll back, what should you do? The same is true here.
