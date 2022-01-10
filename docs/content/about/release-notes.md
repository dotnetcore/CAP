# Release Notes

## Version 6.0.0 (2022-01-06)

**Features:**

* Fully support .NET 6.
* Add support for OpenTelemetry. (#885)
* Improve support for NATS JetStream wildcard topic. (#1047)
* Add support customer header options for Azure Service Bus. (#1063) Thanks [@Mateus Viegas](https://github.com/mviegas)

## Version 5.2.0 (2021-11-12)

**Features:**

* Add support for NATS JetStream. (#983)
* Add support for Apache Pulsar. (#610)
* Add possibility to process messages for each consumer group indepedently. (#1027)

**Bug Fixed:**

* Fixed message content of bigint type cannot be displayed correctly in dashboard. (#1028)
* Fixed unobserved tasks of async method calls in Amazon SQS. (#1033)
* Fixed RabbitMQ federation plugin message header object values cause exceptions. (#1036)

## Version 5.1.2 (2021-07-26)

**Bug Fixed:**

* Fixed consumer register cancellation token source null referencee bug. (#952)
* Fixed redis streams transport cluster keys cross-hashslot bug. (#944)


## Version 5.1.1 (2021-07-09)

**Features:**

* Improve flow control for message cache of in memory. (#935)
* Add cancellation token support to subscribers. (#912)
* Add pathbase options for dashbaord. (#901)
* Add custom authorization scheme support for dashbaord. (#906)

**Bug Fixed:**

* Fixed mysql connect timeout expired bug. (#931)
* Fixed consul health check path invalid bug. (#921)
* Fixed mongo dashbaord query bug. (#909)

## Version 5.1.0 (2021-06-07)

**Features:**

* Add configure options for json serialization. (#879)
* Add Redis Streams transport support. (#817)
* New dashboard build with vue. (#880)
* Add subscribe filter support. (#894)

**Bug Fixed:**

* Fixed use CapEFDbTransaction to get dbtransaction extension method bug. (#868)
* Fixed pending message has not been deleted from buffer list in SQL Server. (#889)
* Fixed dispatcher processing when storage message exception bug. (#900)


## Version 5.0.3 (2021-05-14)

**Bug Fixed:**

* Fix the bug of getting db transaction through the IDbContextTransaction for SQLServer. (#867)
* Fix RabbitMQ Connection close forced. (#861)

## Version 5.0.2 (2021-04-28)

**Features:**

* Add support for Azure Service Bus sessions. (#829)
* Add custom message headers support for RabbitMQ consumer. (#818)

**Bug Fixed:**

* Downgrading Microsoft.Data.SqlClient to 2.0.1. (#839)
* DiagnosticObserver does not use null connection. (#845)
* Fix null reference in AmazonSQSTransport. (#846)

## Version 5.0.1 (2021-04-07)

**Features:**

* Add KafkaOptions.MainConfig to AutoCreateTopic. (#810)
* Add support rewriting the default configuration of Kafka consumer. (#822)
* Add DefaultChallengeScheme dashboard options to specify dashboard auth challenge scheme. (#815)

**Bug Fixed:**
 
* Fixed topic selector in IConsumerServiceSelector. (#806)
* Update AWS topic subscription and SQS access policy generation. (#808)
* Fixed memory leak when using transction to publish message. (#816)
* Fixed SQL content filter on IMonitoringApi.PostgreSql.cs. (#814)
* Fixed the expiration time display problem in the dashboard due to time zone issues (#820)
* Fixed the creation timing of Kafka automatically creating Topic. (#823)
* Fixed Dashboard metric not update. (#819)

## Version 5.0.0 (2021-03-23)
 
**Features:**

* Upgrade to .NET Standard 2.1 and support .NET 5. (#727)
* Replace Newtonsoft.Json to System.Text.Json. (#740)
* Support NATS Transport. (#595,#743)
* Enabling publiser confirms for RabbitMQ. (#730)
* Support query subscription from DI implementation factory. (#756)
* Add options to create lazy queue for RabbitMQ. (#772)
* Support to add custom tags for Consul. (#786)
* Support custom group and topic prefiex. (#780)
* Renemae DefaultGroup option to DefaultGroupName.
* Add auto create topic at startup for Kafka. (#795,#744)

**Bug Fixed:**

* Fixed retrying process earlier than consumer registration to DI. (#760)
* Fixed Amazon SQS missing pagination topics. (#765)
* Fixed RabbitMQ MessageTTL option to int type. (#787)
* Fixed Dashboard auth. (#793)
* Fixed ClientProvidedName could not be renamed for RabbitMQ. (#791)
* Fixed EntityFramework transaction will not rollback when exception occurred. (#798)

## Version 3.1.2 (2020-12-03)

**Features:**
* Support record the exception message in the headers. (#679)
* Support consul service check for https. (#722)
* Support custom producer threads count options for sending. (#731)
* Upgrade dependent nuget packages to latest.

**Bug Fixed:**

* Fixed InmemoryQueue expired messages are not removed bug. (#691)
* Fixed Executor key change lead to possible null reference exception. (#698)
* Fixed Postgresql delete expires data logic error. (#714)

## Version 3.1.1 (2020-09-23)

**Features:**

* Add consumer parameter with interface suppport. (#669)
* Add custom correlation id and message id support. (#668)
* Enhanced custom serialization support. (#641)

**Bug Fixed:**

* Solve the issue of being duplicated executors from different assemblies. (#666)
* Added comparer to remove duplicate ConsumerExecutors. (#653)
* Add re-enable the auto create topics configuration item for Kafka, it's false by default. now is true. (#635)
* Fixed postgresql transaction rollback invoke bug. (#640)
* Fixed SQLServer table name customize bug. (#632)

## Version 3.1.0 (2020-08-15)

**Features:**

* Add Amazon SQS support. (#597)
* Remove Dapper and replace with ADO.NET in storage project. (#583)
* Add debug symbols package to nuget.
* Upgrade dependent nuget package version to latest.
* English docs grammar correction. Thanks @mzorec

**Bug Fixed:**

* Fix mysql transaction rollback bug. (#598)
* Fix dashboard query bug. (#600)
* Fix mongo db query bug. (#611)
* Fix dashboard browser language detection bug. (#631)

## Version 3.0.4 (2020-05-27)

**Bug Fixed:**

* Fix kafka consumer group does not works bug. (#541)
* Fix cast object to primitive types failed bug. (#547)
* Fix subscriber primitive types convert exception. (#568)
* Add conosole app sample.
* Upgrade Confluent.Kafka to 1.4.3


## Version 3.0.3 (2020-04-01)

**Bug Fixed:**

* Change ISubscribeInvoker interface access modifier to public. (#537)
* Fix rabbitmq connection may be reused when close forced. (#533)
* Fix dahsboard message reexecute button throws exception bug. (#525)

## Version 3.0.2 (2020-02-05)

**Bug Fixed:**

- Fixed diagnostics event data object error. (#504 )
- Fixed RabbitMQ transport check not working. (#503 )
- Fixed Azure Service Bus subscriber error. (#502  )

## Version 3.0.1 (2020-01-19)

**Bug Fixed:**

* Fixed Dashboard requeue and reconsume failed bug.  (#482 )
* Fixed Azure service bus null reference exception. (#483 )
* Fixed type cast exception from storage. (#473 )
* Fixed SqlServer  connection undisponse bug. (#477 )

## Version 3.0.0 (2019-12-30)

**Breaking Changes:**

In this version, we have made major improvements to the code structure, which have introduced some destructive changes.

* Publisher and Consumer are not compatible with older versions
This version is not compatible with older versions of the message protocol because we have improved the format in which messages are published and stored.

* Interface changes
We have done a lot of refactoring of the code, and some of the interfaces may be incompatible with older versions

* Detach the dashboard project

**Features:**

* Supports .NET Core 3.1.
* Upgrade dependent packages.
* New serialization interface `ISerializer` to support serialization of message body sent to MQ.
* Add new api for `ICapPublisher` to publish message with headers.
* Diagnostics event structure and names improved. #378
* Support consumer method to read the message headers. #472
* Support rename message storage tables. #435
* Support for Kafka to write such as Offset and Partition to the header. #374
* Improved the processor retry interval time. #444

**Bug Fixed:**

* Fixed SqlServer dashboard sql query bug. #470
* Fixed Kafka health check bug. #436
* Fixed dashboard bugs. #412 #404
* Fixed transaction bug for sql server when using EF. #402


## Version 2.6.0 (2019-08-29)

**Features:**

* Improvement Diagnostic support. Thanks [@gfx687](https://github.com/gfx687) 
* Improvement documention. https://cap.dotnetcore.xyz
* Improvement `ConsumerInvoker` implementation. Thanks [@hetaoos](https://github.com/hetaoos)
* Support multiple consumer threads. (#295)
* Change DashboardMiddleware to async. (#390) Thanks [@liuzhenyulive](https://github.com/liuzhenyulive) 

**Bug Fixed:**

* SQL Server Options Bug.
* Fix transaction scope disposed bug. (#365)
* Fix thread safe issue of ICapPublisher bug. (#371)
* Improved Ctrl+C action raised exception issue.
* Fixed asynchronous exception catching bug of sending.
* Fix MatchPoundUsingRegex "." not escaped bug (#373)

## Version 2.5.1 (2019-06-21)

**Features:**

* Improved logs record.
* Upgrade dependent nuget packages version. (MySqlConnector, confluent-kafka-dotnet-1.0 )
* NodeId type change to string of DiscoveryOptions for Consul. (#314)
* Change the IConsumerServiceSelector interface access modifier to public. (#333)
* Improved RabbitMQOptions to provide extensions option to configure the client original configuration. (#350)
* Add index for MongoDB CAP collections. (#353)

**Bugs Fixed:**

* Fixed consumer re-register transport bug. (#329)
* Handle messages retrieval failure. (#324)
* Fixed DiagnosticListener  null reference exception bug. (#335)
* Add subscription name validation for the AzureServerBus. (#344)
* Fixed thread safety issues of publisher. (#331)

## Version 2.5.0 (2019-03-30)

**Features:**

* Support Azure Service Bus. (#307)
* Support In-Memory Storage. (#296)
* Upgrade Dapper to version 1.60.1
* Support read environment variables CAP_WORKERID and CAP_DATACENTERID as the snowflake algorithm workerid and datacenterid.

**Bug Fixed:**

* Modify MySQL cap table encoding to utf8mb4. (#305)
* Move CapSubscribeAttribute class to DotNetCore.CAP project.
* Fixed multiple instance snowflake algorithm generating primary key conflicts. (#294)

## Version 2.4.2 (2019-01-08)

**Features:**

* Startup the CAP with the .NET Core 2.1 BackgroundService. (#265)
* Improved message delivery performance. #261

**Bug Fixed:**

* Fixed PostgreSql version isolation feature bug. (#256)
* Fixed SQL Server sql bug for dashboard search. (#266)

## Version 2.4.1 (2018-12-19)

**Bug Fixed:**

* Fixed MongoDB version isolation feature bug. (#253)

## Version 2.4.0 (2018-12-08)

**Features:**

* Supported version options. (#220)
* Upgrade nuget package to .net core 2.2.

**Breaking Changes:**

In order to support the "version isolation" feature, we introduced a new version field in version 2.4.0 to isolate different versions of the message, so this requires some adjustments to the database table structure. You can use the following SQL to add a version field to your database CAP related table.

**MySQL**
```sql
ALTER TABLE `cap.published` ADD Version VARCHAR(20) NULL;
ALTER TABLE `cap.received` ADD Version VARCHAR(20) NULL;
```

**SQL Server**
```sql
ALTER TABLE Cap.[Published] ADD Version VARCHAR(20) NULL;
ALTER TABLE Cap.[Received] ADD Version VARCHAR(20) NULL;
```

**PostgreSQL**
```sql
ALTER TABLE cap.published ADD  "Version" VARCHAR(20) NULL;
ALTER TABLE cap.received ADD "Version" VARCHAR(20) NULL;
```

**MongoDb**
```
db.CapPublishedMessage.update({},{"$set" : {"Version" : "1"}});
db.CapReceivedMessage.update({},{"$set" : {"Version" : "1"}});
```

**Bug Fixed:**

- Fixed different groups of the same topic name in one instance will cause routing bug. (#235)
- Fixed message presistence bug. (#240)
- Fixed RabbitMQ topic name contains numbers will cause exception bug. (#181)

## Version 2.3.1 (2018-10-29)

**Features:**

- Add Source Link Support
- Upgrade dependent NuGet packages.

**Bug Fixed:**

- Fixed dashboard messages requeue error. (#205)
- Adjustment snowflake workerId to random id.
- Fixed flush unclaer data bug.

## Version 2.3.0 (2018-08-30)

In this version, we made some breaking changes for the publisher API, you can see this blog to understand the story behind.

If you have any migration question, please comment in issue (#190).

**Breaking Changes:**

- Removed app.UseCap() from Startup.cs
- Message table primary key data type has been modified to Bigint and non auto-Increment. (#180)
- New publisher Api. (#188)

**Features:**

- MongoDb supported. (#143)
- Automatic commit transaction. (#191)

**Bug Fixed:**

- Fix message still sent if transaction faild bug. (#118)
- Multiple events in one transaction. (#171)

## Version 2.2.5 (2018-07-19)

**Features:**
- Performance improvement

**Bug Fixed:**

- Fixed message enqueue exception.
- Fixed Retry processor bugs.
- Fixed Kafka producer exception log without logging when publish message.
- Fixed Incorrect local IP address judgment of IPv6. (#140)
- Fixed DateTime localization format conversion error to sql. (#139)
- Fixed dashboard message page re-requeue and re-executed operate bug. (#158)
- Fixed SendAsync or ExecuteAsync recursion retries bug. (#160)
- Fixed configuration options of FailedThresholdCallback could not be invoke when the value less then three. (#161)

## Version 2.2.4 (2018-06-05)

Because version 2.2.3 was not released to nuget, so released 2.2.4.

## Version 2.2.3 (2018-06-05)

**Features:**

- Improved log output.
- Upgrade nuget packages.
- Support pattern matching for consumer. (#132)

**Bug Fixed:**

- Fixed exception thrown when terminate the program with Ctrl+C. (#130)

## Version 2.2.2 (2018-04-28)

**Features:**

- Improved log output. #114
- Add default timeout configuration for kafka client.
- Rename configuration options FailedCallback to FailedThresholdCallback.

**Bug Fixed:**

- Fixed message enqueue exception.
- Fixed retry processor bugs.
- Fixed kafka producer exception log without logging when publish message.

## Version 2.2.1 (2018-04-18)

**Bug Fixed:**

- Fixed message enqueue bug in v2.2


## Version 2.2.0 (2018-04-17)

**Features:**

- Remove database queue mode. (#102)
- Support for Diagnostics. (#112)
- Upgrade dependent nuget packages.

**Bug Fixed:**

- Fixed bug of the FailedRetryCount does not increase when raised SubscribeNotFoundException. (#90)

## Version 2.1.4 (2018-03-16)

**Features:**

- Remove TableNamePrefix option from MySqlOptions to EFOptions.
- Upgrade nuget package

**Bug Fixed:**

- Fixed the connection bug of getting message from table. (#83)
- Fixed entityframework rename table name prefix bug. (#84)
- Fixed sql server scripts bug of create table scheme. (#85)
- Fixed thread safety issue about KafkaOptions.(#89)

## Version 2.1.3 (2018-01-24)

**Features:**

- Upgrade dependent nuget packages version.
- NuGet package include xml doc now.
- NuGet now contains the CAP symbol files.

**Bug Fixed:**

- Fixed thread conflict issue when sending messages with PublishAsync. (#80)
- Fixed kafka received message sava failed may caused the mssage loss bug. (#78)
- Fixed dashboard js syntax issue. (#77)

## Version 2.1.2 (2017-12-18)

**Bug Fixed:**

- Fixed and improve the performance of mysql processing messages. (#68) (#36)
- Fixed dashboard manually trigger reconsumption bug. (#67)
- Fixed mysql 5.5 table initialization bug. (#65)
- Fixed mysql message queue executor bug. (#66)

## Version 2.1.1 (2017-11-28)

**Bug Fixed:**

- Fixed 'dotnet.exe' process incomplete quit when shutdown application (Ctrl+C). (#64)
- Fixed failure to issue as expected of RabbitMQ SubscriberNotFoundException. (#63)
- Fixed Sent async message in the loop causes an exception. (#62)

## Version 2.1.0 (2017-11-17)

**Features:**

- Interface display optimization of dashboard.
- Adds a more friendly display when looks at the message content.
- Now you can see the exception infomation in the message conent filed when message send or executed failed.
- Optimize LAN to see Dashboard without authentication.
- Add IContentSerializer interface, you can customize the serialized message content.
- Add IMessagePacker interface, you can customize wapper of the message.
- Upgrade the dependent package.

**Bug Fixed:**

- Fixed dashboard query bugs.
- Fixed dashboard multilanguage display bugs.
- Fixed RabbitMQ connection pool bug.
- Fixed dashboard display bugs on mobile.

## Version 2.0.2 (2017-09-29)

**Bug Fixed:**

- Fixed asp.net core 2.0 startup error of MySql and PostgreSql. (#44

## Version 2.0.1 (2017-09-16)

**Bug Fixed:**

- DbContext services bug. (#44)
- Dependency injection bug. (#45)

## Version 2.0.0 (2017-09-01)

**Features:**

* Supported .net standard 2.0.
* Supported PostgreSQL 9.5+.
* Supported asynchronous function subscriptions.
* `ICapPublisher` api supported callback subsrciber.

**Bug Fixed:**

* Fixed multiple subscriber subscribe bug. (#38)
* Fixed model binde bug. (#17) (#18)
* Fixed database connection disposed bug. (#25)
* Fixed consumer method injection context bug. (#34)

## Version 1.1.0 (2017-08-04)

**Features:**

- Support MySQL database persistent message.
- Add message failed call-back in CapOptions.
- Remove publish messages API of string name at `ICapPublisher`.

**Bug Fixed:**

- Fixed can not send message for string type. (#17)
- Fixed model bind for type like datetime guid always failed. (#18)

## Version 1.0.1 (2017-07-25)

**Features:**

- ICapPublisher interface added synchronous publish API.
- Add infinity retry failed processor.

## Version 1.0.0 (2017-07-19)

- Project published