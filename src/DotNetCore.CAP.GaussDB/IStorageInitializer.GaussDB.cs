using DotNetCore.CAP.Persistence;
using HuaweiCloud.GaussDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.GaussDB
{
    /// <summary>
    /// GaussDB 存储初始化器，负责等待目标数据库可用并创建 CAP 所需的 Schema、表、索引和锁记录。
    /// </summary>
    public class GaussDBStorageInitializer : IStorageInitializer
    {
        private DbConnectionExtensions.GaussDBCompatibilityMode _databaseCompatibilityMode;
        private readonly IOptions<CapOptions> _capOptions;
        private readonly ILogger _logger;
        private readonly IOptions<GaussDBOptions> _options;

        /// <summary>
        /// 初始化 GaussDB 存储初始化器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="options">GaussDB 连接和启动配置。</param>
        /// <param name="capOptions">CAP 全局配置。</param>
        public GaussDBStorageInitializer(
            ILogger<GaussDBStorageInitializer> logger,
            IOptions<GaussDBOptions> options,
            IOptions<CapOptions> capOptions)
        {
            _logger = logger;
            _options = options;
            _capOptions = capOptions;
            // 默认按 PostgreSQL 兼容模式生成对象名，连接成功后会再按实际兼容模式刷新。
            _databaseCompatibilityMode = DbConnectionExtensions.GaussDBCompatibilityMode.PostgreSQL;
        }

        /// <summary>
        /// 数据库兼容模式
        /// </summary>
        public virtual int DBCompatibilityMode => (int)_databaseCompatibilityMode;

        /// <summary>
        /// 获取 CAP 发布消息表的完整限定名。
        /// </summary>
        public virtual string GetPublishedTableName()
        {
            if (_databaseCompatibilityMode != DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility) return $"\"{_options.Value.Schema}\".\"published\"";
            return $"`{_options.Value.Schema}`.`published`";
        }

        /// <summary>
        /// 获取 CAP 接收消息表的完整限定名。
        /// </summary>
        public virtual string GetReceivedTableName()
        {
            if (_databaseCompatibilityMode != DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility) return $"\"{_options.Value.Schema}\".\"received\"";
            return $"`{_options.Value.Schema}`.`received`";
        }

        /// <summary>
        /// 获取 CAP 分布式存储锁表的完整限定名。
        /// </summary>
        public virtual string GetLockTableName()
        {
            if (_databaseCompatibilityMode != DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility) return $"\"{_options.Value.Schema}\".\"lock\"";
            return $"`{_options.Value.Schema}`.`lock`";
        }

        /// <summary>
        /// 初始化 GaussDB 存储对象；只有确认目标数据库存在后才会继续建表。
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            // 确保数据库是存在的
            await WaitUntilDatabaseExistsAsync(cancellationToken).ConfigureAwait(false);
            // 开始初始化
            await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 执行实际建表脚本，并按数据库兼容模式选择对象引用和 SQL 语法。
        /// </summary>
        protected virtual async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new GaussDBParameter("@PubKey", $"publish_retry_{_capOptions.Value.Version}"),
                new GaussDBParameter("@RecKey", $"received_retry_{_capOptions.Value.Version}"),
                new GaussDBParameter("@LastLockTime", DateTime.MinValue)
            };
            _databaseCompatibilityMode = await connection.GetGaussDBCompatibilityModeAsync();
            var sql = CreateDbTablesScript(_options.Value.Schema);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);

            _logger.LogDebug("Ensuring all GaussDB database tables are created.");
        }

        /// <summary>
        /// 创建 CAP 存储初始化脚本。
        /// </summary>
        /// <param name="schema">CAP 使用的数据库 Schema。</param>
        /// <returns>可直接执行的 Schema、表、索引和锁记录初始化 SQL。</returns>
        protected virtual string CreateDbTablesScript(string schema)
        {
            if (_databaseCompatibilityMode != DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility)
            {
                #region 非 MySQL Compatibility 模式
                var sqlBuilder = new StringBuilder($@"
DO
$$
    BEGIN
        IF NOT EXISTS(SELECT schema_name FROM information_schema.schemata WHERE schema_name = '{schema}')
        THEN
            EXECUTE 'CREATE SCHEMA ""{schema}"";';
        END IF;
    END
$$;

CREATE TABLE IF NOT EXISTS {GetPublishedTableName()} (
    ""Id""         BIGINT       NOT NULL,
    ""Version""    VARCHAR(20)  NOT NULL,
    ""Name""       VARCHAR(200) NOT NULL,
    ""Content""    TEXT,
    ""Retries""    INTEGER      NOT NULL,
    ""Added""      TIMESTAMP    NOT NULL,
    ""ExpiresAt""  TIMESTAMP,
    ""StatusName"" VARCHAR(50)  NOT NULL,
    PRIMARY KEY (""Id"")
);
CREATE INDEX IF NOT EXISTS ""idx_published_ExpiresAt_StatusName"" ON {GetPublishedTableName()} (""ExpiresAt"", ""StatusName"");
CREATE INDEX IF NOT EXISTS ""idx_published_Version_ExpiresAt_StatusName"" ON {GetPublishedTableName()} (""Version"", ""ExpiresAt"", ""StatusName"");

CREATE TABLE IF NOT EXISTS {GetReceivedTableName()} (
    ""Id""         BIGINT       NOT NULL,
    ""Version""    VARCHAR(20)  NOT NULL,
    ""Name""       VARCHAR(200) NOT NULL,
    ""Group""      VARCHAR(200),
    ""Content""    TEXT,
    ""Retries""    INTEGER      NOT NULL,
    ""Added""      TIMESTAMP    NOT NULL,
    ""ExpiresAt""  TIMESTAMP,
    ""StatusName"" VARCHAR(50)  NOT NULL,
    PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""idx_received_ExpiresAt_StatusName""  ON {GetReceivedTableName()} (""ExpiresAt"", ""StatusName"");
CREATE INDEX IF NOT EXISTS ""idx_received_Version_ExpiresAt_StatusName"" ON {GetReceivedTableName()} (""Version"", ""ExpiresAt"", ""StatusName"");
    ");

                if (_capOptions.Value.UseStorageLock)
                {
                    sqlBuilder.AppendLine($@"
CREATE TABLE IF NOT EXISTS {GetLockTableName()} (
    ""Key""          VARCHAR(128) NOT NULL,
    ""Instance""     VARCHAR(256),
    ""LastLockTime"" TIMESTAMP    NOT NULL,
    PRIMARY KEY (""Key"")
);

INSERT INTO {GetLockTableName()} (""Key"",""Instance"",""LastLockTime"")
SELECT @PubKey, '', @LastLockTime
WHERE NOT EXISTS (SELECT 1 FROM {GetLockTableName()} WHERE ""Key"" = @PubKey);

INSERT INTO {GetLockTableName()} (""Key"",""Instance"",""LastLockTime"")
SELECT @RecKey, '', @LastLockTime
WHERE NOT EXISTS (SELECT 1 FROM {GetLockTableName()} WHERE ""Key"" = @RecKey);
    ");
                }

                return sqlBuilder.ToString();

                #endregion
            }
            else
            {
                // M-Compatibility 模式使用 MySQL 风格反引号、datetime 类型和 INSERT IGNORE。
                #region M-Compatibility 模式
                var sqlBuilder = new StringBuilder($@"
CREATE SCHEMA IF NOT EXISTS `{schema}`;

CREATE TABLE IF NOT EXISTS {GetReceivedTableName()} (
  `Id` bigint NOT NULL,
  `Version` varchar(20) DEFAULT NULL,
  `Name` varchar(400) NOT NULL,
  `Group` varchar(200) DEFAULT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_Version_ExpiresAt_StatusName` (`Version`, `ExpiresAt`, `StatusName`),
  INDEX `IX_ExpiresAt_StatusName` (`ExpiresAt`, `StatusName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS {GetPublishedTableName()} (
  `Id` bigint NOT NULL,
  `Version` varchar(20) DEFAULT NULL,
  `Name` varchar(200) NOT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(40) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_Version_ExpiresAt_StatusName` (`Version`, `ExpiresAt`, `StatusName`),
  INDEX `IX_ExpiresAt_StatusName` (`ExpiresAt`, `StatusName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    ");
                if (_capOptions.Value.UseStorageLock)
                {
                    sqlBuilder.AppendLine($@"
CREATE TABLE IF NOT EXISTS {GetLockTableName()} (
  `Key` varchar(128) NOT NULL,
  `Instance` varchar(256) DEFAULT NULL,
  `LastLockTime` datetime DEFAULT NULL,
  PRIMARY KEY (`Key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO {GetLockTableName()} (`Key`,`Instance`,`LastLockTime`)
SELECT @PubKey, '', @LastLockTime
WHERE NOT EXISTS (SELECT 1 FROM {GetLockTableName()} WHERE `Key` = @PubKey);

INSERT IGNORE INTO {GetLockTableName()} (`Key`,`Instance`,`LastLockTime`)
SELECT @RecKey, '', @LastLockTime
WHERE NOT EXISTS (SELECT 1 FROM {GetLockTableName()} WHERE `Key` = @RecKey);
    ");
                }
                return sqlBuilder.ToString();
                #endregion
            }
        }

        /// <summary>
        /// 等待下一次数据库存在性探测；测试可重写该方法避免真实延时。
        /// </summary>
        protected virtual Task DelayBeforeDatabaseExistsRetryAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }

        /// <summary>
        /// 等待目标数据库创建完成；适用于 EF 迁移或外部初始化尚未完成的启动阶段。
        /// </summary>
        /// <exception cref="InvalidOperationException">超过最大重试次数后目标数据库仍不存在。</exception>
        private async Task WaitUntilDatabaseExistsAsync(CancellationToken cancellationToken)
        {
            var connection = _options.Value.CreateConnection();
            var databaseName = new GaussDBConnectionStringBuilder(connection.ConnectionString).Database;

            try
            {
                var maxRetries = _options.Value.StartupCheckDatabaseExistsMaxRetries;
                for (var retry = 0; retry <= maxRetries; retry++)
                {
                    var exsit = await connection.DataBaseIsExistsAsync(_options.Value.AdminDatabaseName).ConfigureAwait(false);
                    if (exsit) return;

                    var delay = ComputeBackoffDelay(retry, _options.Value.StartupCheckDatabaseExistsBaseDelay, _options.Value.StartupCheckDatabaseExistsMaxDelay);

                    _logger.LogWarning("[{CountThis}´th] GaussDB database '{Database}' does not exist.{CountNext}´th Retrying in {DelaySeconds} seconds.",
                      retry + 1,
                      databaseName,
                      retry + 2,
                      delay.TotalSeconds);

                    await DelayBeforeDatabaseExistsRetryAsync(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection?.Dispose();
            }

            throw new InvalidOperationException($"GaussDB database '{databaseName}' does not exist.");
        }

        /// <summary>
        /// 按指数退避计算下一次数据库存在性探测的等待时间。
        /// </summary>
        /// <param name="attempt">当前尝试序号，从 0 开始。</param>
        /// <param name="baseDelay">基础等待间隔。</param>
        /// <param name="maxDelay">允许的最大等待间隔。</param>
        /// <returns>本次重试前需要等待的时间。</returns>
        /// <remarks>
        /// 当基础间隔为 1 秒时，等待序列为 1s、1s、2s、4s，并持续翻倍直到达到最大间隔。
        /// </remarks>
        private static TimeSpan ComputeBackoffDelay(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
        {
            if (attempt <= 0) return baseDelay;

            double factor = Math.Pow(2, attempt - 1);
            double millis = baseDelay.TotalMilliseconds * factor;

            if (millis < 0 || millis > maxDelay.TotalMilliseconds || double.IsInfinity(millis) || double.IsNaN(millis))
            {
                millis = maxDelay.TotalMilliseconds;
            }

            return TimeSpan.FromMilliseconds(millis);
        }
    }
}
