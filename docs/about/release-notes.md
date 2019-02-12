# Release Notes

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

If you have any migration question, please comment in issue #190.

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
