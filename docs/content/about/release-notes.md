# Release Notes

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