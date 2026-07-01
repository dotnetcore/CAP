using System;

namespace DotNetCore.CAP
{
    /// <summary>
    /// GaussDB 存储与 EF Core 集成共用的基础配置。
    /// </summary>
    public class EFOptions
    {
        /// <summary>
        /// 默认的数据库 Schema 名称。
        /// </summary>
        public const string DefaultSchema = "cap";

        /// <summary>
        /// 实际使用的数据库 Schema 名称。
        /// </summary>
        public string Schema { get; set; } = DefaultSchema;

        /// <summary>
        /// 通过 EF Core 配置 CAP 存储时绑定的 DbContext 类型。
        /// </summary>
        internal Type? DbContextType { get; set; }

        /// <summary>
        /// CAP 当前应用版本，用于隔离不同版本的消息和锁记录。
        /// </summary>
        internal string Version { get; set; } = default!;
    }
}
