using System;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// CAP 对外暴露的 GaussDB 存储配置扩展方法。
    /// </summary>
    public static class CapOptionsExtensions
    {
        /// <summary>
        /// 使用连接字符串启用 GaussDB 作为 CAP 存储。
        /// </summary>
        public static CapOptions UseGaussDB(this CapOptions options, string connectionString)
        {
            return options.UseGaussDB(opt => opt.ConnectionString = connectionString);
        }

        /// <summary>
        /// 使用自定义配置启用 GaussDB 作为 CAP 存储。
        /// </summary>
        public static CapOptions UseGaussDB(this CapOptions options, Action<GaussDBOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            // CAP Version 需要写入存储配置，后续消息查询和锁记录会按版本隔离。
            configure += x => x.Version = options.Version;
            options.RegisterExtension(new GaussDBCapOptionsExtension(configure));
            return options;
        }

        /// <summary>
        /// 从指定 DbContext 的 EF Core provider 配置中解析 GaussDB 连接信息。
        /// </summary>
        public static CapOptions UseEntityFramework<TContext>(this CapOptions options)
            where TContext : DbContext
        {
            return options.UseEntityFramework<TContext>(_ => { });
        }

        /// <summary>
        /// 从指定 DbContext 解析连接信息，并允许覆盖 Schema 等 EF 相关配置。
        /// </summary>
        public static CapOptions UseEntityFramework<TContext>(this CapOptions options, Action<EFOptions> configure)
            where TContext : DbContext
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new GaussDBCapOptionsExtension(x =>
            {
                configure(x);
                x.Version = options.Version;
                x.DbContextType = typeof(TContext);
            }));
            return options;
        }
    }
}
