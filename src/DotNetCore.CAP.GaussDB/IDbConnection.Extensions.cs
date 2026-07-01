using HuaweiCloud.GaussDB;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace DotNetCore.CAP.GaussDB
{
    /// <summary>
    /// GaussDB ADO.NET 连接的轻量级执行扩展，集中处理打开连接、参数绑定和兼容模式探测。
    /// </summary>
    internal static class DbConnectionExtensions
    {
        private static readonly ConcurrentDictionary<string, string> CompatibilityModeCache = new();

        /// <summary>
        /// 执行不返回结果集的 SQL，并在连接关闭时自动打开连接。
        /// </summary>
        public static async Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql,
            DbTransaction? transaction = null, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed) await connection.OpenAsync().ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var parameter in sqlParams) command.Parameters.Add(parameter);
            if (transaction != null) command.Transaction = transaction;
            return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// 执行查询 SQL，并把 DbDataReader 的读取逻辑交给调用方。
        /// </summary>
        public static async Task<T> ExecuteReaderAsync<T>(this DbConnection connection, string sql,
            Func<DbDataReader, Task<T>> readerFunc, DbTransaction? transaction = null, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed) await connection.OpenAsync().ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var parameter in sqlParams) command.Parameters.Add(parameter);
            if (transaction != null) command.Transaction = transaction;
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            return await readerFunc(reader).ConfigureAwait(false);
        }

        /// <summary>
        /// 执行标量查询，并把数据库返回值转换成调用方需要的类型。
        /// </summary>
        public static async Task<T> ExecuteScalarAsync<T>(this DbConnection connection, string sql,
            params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed) await connection.OpenAsync().ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var parameter in sqlParams) command.Parameters.Add(parameter);
            var value = await command.ExecuteScalarAsync().ConfigureAwait(false);
            if (value == null || value == DBNull.Value) return default!;

            var converter = TypeDescriptor.GetConverter(typeof(T));
            return converter.CanConvertFrom(value.GetType())
                ? (T)converter.ConvertFrom(value)!
                : (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// 通过管理数据库查询当前连接字符串中的目标数据库是否已经存在。
        /// </summary>
        /// <param name="connection">包含目标数据库名的业务连接。</param>
        /// <param name="adminDatabase">用于查询 pg_database 的管理数据库，默认 postgres。</param>
        /// <returns>目标数据库存在返回 true；连接或查询失败时返回 false。</returns>
        public static async Task<bool> DataBaseIsExistsAsync(this DbConnection connection, string adminDatabase = "postgres")
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            try
            {
                var builder = new GaussDBConnectionStringBuilder(connection.ConnectionString);
                var databaseName = builder.Database ?? string.Empty;
                if (string.IsNullOrWhiteSpace(databaseName)) return false;

                builder.Database = string.IsNullOrWhiteSpace(adminDatabase) ? "postgres" : adminDatabase;
                using var adminConnection = new GaussDBConnection(builder.ToString());
                var result = await ExecuteScalarAsync<bool>(adminConnection,
                    @"SELECT EXISTS (SELECT datname FROM pg_catalog.pg_database WHERE datname = @dbname) AS IsExists;",
                    new GaussDBParameter("@dbname", databaseName));

                return true.Equals(result);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取当前连接对应数据库的兼容模式，并映射为 provider 内部枚举。
        /// </summary>
        public static async Task<GaussDBCompatibilityMode> GetGaussDBCompatibilityModeAsync(this DbConnection connection)
        {
            var compatibilityMode = await QuerytGaussDBCompatibilityModeAsync(connection);
            switch ((compatibilityMode ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "PG":
                    return GaussDBCompatibilityMode.PostgreSQL;
                case "A":
                case "ORA":
                    return GaussDBCompatibilityMode.Oracle;
                case "B":
                case "MYSQL":
                case "M":
                    return GaussDBCompatibilityMode.MySQL;
                case "C":
                case "TD":
                    return GaussDBCompatibilityMode.Teradata;
                default:
                    return GaussDBCompatibilityMode.Oracle;
            }
        }

        /// <summary>
        /// 查询当前数据库的 GaussDB 兼容模式。
        /// <list type="table">
        /// <listheader>
        /// <term>返回值</term>
        /// <description>数据库类型</description>
        /// </listheader>
        /// <item><term>A、ORA</term><description>Oracle 兼容模式</description></item>
        /// <item><term>B、MYSQL</term><description>MySQL 兼容模式，通常依赖 dolphin 扩展</description></item>
        /// <item><term>C、TD</term><description>Teradata 兼容模式</description></item>
        /// <item><term>PG</term><description>PostgreSQL 兼容模式</description></item>
        /// <item><term>M</term><description>GaussDB M-Compatibility 模式</description></item>
        /// </list>
        /// </summary>
        internal static async Task<string> QuerytGaussDBCompatibilityModeAsync(this DbConnection connection)
        {
            if (connection is not GaussDBConnection gaussdbConnection) return string.Empty;
            var cacheKey = GetFeatureCacheKey(gaussdbConnection.ConnectionString);
            if (CompatibilityModeCache.TryGetValue(cacheKey, out string? cachedValue)) return cachedValue ?? string.Empty;

            var bilder = new GaussDBConnectionStringBuilder(gaussdbConnection.ConnectionString);
            using var modeConnection = new GaussDBConnection(bilder.ToString());
            var result = await modeConnection.ExecuteScalarAsync<string>("SELECT datcompatibility FROM pg_catalog.pg_database WHERE datname = current_database();",
               new GaussDBParameter("@Database", gaussdbConnection.Database));

            var mode = result?.ToString() ?? string.Empty;
            CompatibilityModeCache[cacheKey] = mode;
            return mode;
        }

        /// <summary>
        /// 获取连接字符串的缓存特征，避免同一实例和数据库重复查询兼容模式。
        /// </summary>
        internal static string GetFeatureCacheKey(string connectionString)
        {
            var builder = new GaussDBConnectionStringBuilder(connectionString);
            return $"Host={builder.Host};Port={builder.Port};Database={builder.Database}";
        }

        /// <summary>
        /// GaussDB 数据库兼容模式。
        /// </summary>
        internal enum GaussDBCompatibilityMode
        {
            /// <summary>
            /// Oracle 兼容模式。
            /// </summary>
            Oracle,

            /// <summary>
            /// MySQL 兼容模式。
            /// </summary>
            MySQL,

            /// <summary>
            /// Teradata 兼容模式。
            /// </summary>
            Teradata,

            /// <summary>
            /// PostgreSQL 兼容模式。
            /// </summary>
            PostgreSQL,

            /// <summary>
            /// GaussDB M-Compatibility 模式。
            /// </summary>
            M_Compatibility
        }
    }
}
