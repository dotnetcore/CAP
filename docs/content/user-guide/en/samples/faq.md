# FAQ

!!! faq "Is there an IM group (e.g., Tencent QQ group) to learn and chat about CAP?"

    There is not. Instead of spending time in IM groups, I encourage developers to develop independent thinking skills and solve problems using the documentation. You can also create issues or send emails if problems persist.

!!! faq "Does each application need a separate database for producer and consumer in CAP?"

    Not necessarily. A recommendation is to use a dedicated database for each application. However, see the Q&A below for alternatives.

!!! faq "How can I use the same database for different applications?"
    
    Define a table prefix name in the `ConfigureServices` method.
    
    Code example:

    ```c#
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCap(x =>
        {
            x.UseKafka("");
            x.UseMySql(opt =>
            {
                opt.ConnectionString = "connection string";
                opt.TableNamePrefix = "appone"; // Use different table name prefix here
            });
        });
    }
    ```

!!! faq "Can CAP avoid using the database for event storage? I just want to send messages."

    Not yet. CAP's purpose is to ensure consistency in microservice or SOA architectures. The solution is based on ACID features of the database. There's no point in a simple message queue wrapper without database support.

!!! faq "If the consumer fails, can I roll back the SQL executed by the producer?"

    No, you cannot roll back. CAP provides eventual consistency, not immediate rollback.

    Consider a scenario where you call a third-party payment service. If you successfully call Alipay's interface but your own code fails afterward, will Alipay roll back? If not, what should you do? The same principle applies here.
