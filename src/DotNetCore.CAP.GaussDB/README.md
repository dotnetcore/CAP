# DotNetCore.CAP.GaussDB

[![NuGet](https://img.shields.io/nuget/v/DotNetCore.CAP.GaussDB.svg)](https://www.nuget.org/packages/DotNetCore.CAP.GaussDB/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

DotNetCore.CAP.GaussDB 是 [CAP](https://cap.dotnetcore.xyz) 分布式事务/EventBus 框架的 **华为云 GaussDB 存储提供程序**，为 CAP 提供基于 GaussDB 数据库的消息持久化、分布式锁和监控能力。

---

## 特性

- 支持 GaussDB 多种数据库兼容模式
- 支持 ADO.NET 直接连接与 EF Core 绑定两种配置方式
- 内置分布式存储锁，保障多实例下的任务安全调度
- 兼容 CAP 全部功能：发布/订阅、延迟消息、失败重试、过期清理、Dashboard 监控
- 跟随 CAP 面向 `.NET 8`、`.NET 9`、`.NET 10` 发布

---

## 支持的 GaussDB 兼容模式

| 兼容模式 | 数据库示例 | 说明 |
|:---|:---|:---|
| **Oracle** | DB_A | `datcompatibility = 'A'` 或 `'ORA'` |
| **MySQL** | DB_B | `datcompatibility = 'B'` 或 `'MYSQL'` |
| **Teradata** | DB_C | `datcompatibility = 'C'` 或 `'TD'` |
| **PostgreSQL** | DB_PG | `datcompatibility = 'PG'` |
| **M-Compatibility** | — | `datcompatibility = 'M'`（MySQL 风格反引号语法，暂不测试） |

启动时会自动探测数据库兼容模式，根据模式选择对应的 SQL 方言（标识符引用、数据类型、时间函数等）。

---

## 安装

```bash
dotnet add package DotNetCore.CAP.GaussDB
```

> 依赖：[HuaweiCloud.GaussDB.Driver](https://www.nuget.org/packages/HuaweiCloud.GaussDB.Driver/) 作为 ADO.NET 驱动。

---

## 快速开始

### 1. 使用连接字符串（ADO.NET）

```csharp
services.AddCap(x =>
{
    x.UseGaussDB("host=192.168.1.63;port=25432;uid=root;pwd=your_password;Database=mydb;sslmode=Disable");
    x.UseRabbitMQ("...");  // 任选一种消息队列传输器
});
```

### 2. 使用配置委托

```csharp
services.AddCap(x =>
{
    x.UseGaussDB(opt =>
    {
        opt.ConnectionString = "host=192.168.1.63;port=25432;uid=root;pwd=your_password;Database=mydb";
        opt.Schema = "cap";                     // 默认值 "cap"
        opt.AdminDatabaseName = "postgres";     // 启动时用于探测数据库存在的管理库
        opt.StartupCheckDatabaseExistsMaxRetries = 5;   // 数据库存在性探测重试次数
        opt.StartupCheckDatabaseExistsBaseDelay = TimeSpan.FromSeconds(1);  // 基础重试间隔
        opt.StartupCheckDatabaseExistsMaxDelay = TimeSpan.FromMinutes(1);   // 最大重试间隔
    });
});
```

### 3. 使用 EF Core（从 DbContext 自动读取连接信息）

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseGaussDB("host=192.168.1.63;port=25432;uid=root;pwd=your_password;Database=mydb"));

services.AddCap(x =>
{
    x.UseEntityFramework<AppDbContext>(ef =>
    {
        ef.Schema = "cap";  // 可覆盖 Schema
    });
    x.UseRabbitMQ("...");
});
```

---

## 事务集成

CAP GaussDB 支持将数据库事务与消息发布绑定，确保业务数据与消息原子一致。

### ADO.NET 事务

```csharp
using var connection = new GaussDBConnection(connectionString);
var publisher = serviceProvider.GetRequiredService<ICapPublisher>();

var transaction = connection.BeginTransaction(publisher);
// ... 执行业务 SQL，使用同一个 transaction ...
transaction.Commit();  // 数据库提交后自动 Flush CAP 消息队列
```

### EF Core 事务

```csharp
await using var context = new AppDbContext();
var publisher = serviceProvider.GetRequiredService<ICapPublisher>();

var transaction = await context.Database.BeginTransactionAsync(publisher);
// ... EF Core 业务操作 ...
await transaction.CommitAsync();  // 提交后自动 Flush 消息
```

### 事务行为

| 操作 | 数据库行为 | CAP 消息行为 |
|:---|:---|:---|
| `Commit()` | 提交数据库事务 | 刷新待发布消息到队列 |
| `Rollback()` | 回滚数据库事务 | 消息不发送 |

---

## 配置参数

### GaussDBOptions

| 参数 | 类型 | 默认值 | 说明 |
|:---|:---|:---|:---|
| `ConnectionString` | `string?` | `null` | GaussDB 连接字符串 |
| `DataSource` | `GaussDBDataSource?` | `null` | 复用外部 GaussDB 数据源（优先级高于 ConnectionString） |
| `Schema` | `string` | `"cap"` | 存储 CAP 消息表的 Schema 名 |
| `AdminDatabaseName` | `string` | `"postgres"` | 启动时用于探测目标数据库是否存在的管理数据库 |
| `EnableAutoSetNoResetOnClose` | `bool` | `true` | 是否自动追加 `No Reset On Close=True`，避免连接池归还时重置会话状态 |
| `StartupCheckDatabaseExistsMaxRetries` | `int` | `5` | 启动时目标数据库存在性探测最大重试次数 |
| `StartupCheckDatabaseExistsBaseDelay` | `TimeSpan` | `1s` | 每次重试的基础等待间隔（按指数退避翻倍） |
| `StartupCheckDatabaseExistsMaxDelay` | `TimeSpan` | `1min` | 单次重试等待间隔上限 |

---

## 存储结构

GaussDB 存储提供程序会在指定 Schema 下创建三张表：

| 表 | 用途 |
|:---|:---|
| `{schema}.published` | 发布消息记录（Id、Name、Content、StatusName 等） |
| `{schema}.received` | 接收消息记录（额外包含 Group 字段） |
| `{schema}.lock` | 分布式锁记录（仅在 `UseStorageLock=true` 时创建） |

---

## 兼容模式 SQL 方言

不同兼容模式下，SQL 语法自动适配：

| 特性 | Oracle/MySQL/TD/PG | M-Compatibility |
|:---|:---|:---|
| 标识符引用 | `"schema"."table"` | `` `schema`.`table` `` |
| 参数占位 | `@Param` | `@Param` |
| 行锁 | `FOR UPDATE SKIP LOCKED` | `FOR UPDATE` |
| 时间累加 | `"Col" + interval 'N' second` | `date_add(col, interval N second)` |
| 小时聚合 | `to_char("Added", 'yyyy-MM-dd-HH')` | `DATE_FORMAT(Added, '%Y-%m-%d-%H')` |
| 数据类型 | `TIMESTAMP` / `TEXT` | `datetime` / `longtext` |
| 存储引擎 | — | `ENGINE=InnoDB` |

---

## 依赖

- [HuaweiCloud.GaussDB.Driver](https://www.nuget.org/packages/HuaweiCloud.GaussDB.Driver/) ≥ 8.0.1 — GaussDB ADO.NET 驱动
- [Microsoft.EntityFrameworkCore.Relational](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/) — EF Core ORM（可选）
- [HuaweiCloud.EntityFrameworkCore.GaussDB](https://www.nuget.org/packages/HuaweiCloud.EntityFrameworkCore.GaussDB/) — GaussDB EF Core Provider（可选，使用 EF 时需要）

---

## 许可证

本项目基于 MIT 协议开源，详见 [LICENSE](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)。
