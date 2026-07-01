using System;
using System.Linq;
using System.Text.RegularExpressions;
using DotNetCore.CAP.Internal;
using HuaweiCloud.GaussDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    /// <summary>
    /// GaussDB 存储提供程序的连接和启动检查配置。
    /// </summary>
    public class GaussDBOptions : EFOptions
    {
        /// <summary>
        /// 直接连接 GaussDB 的连接字符串。
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// 复用外部创建的 GaussDB 数据源；优先级高于连接字符串。
        /// </summary>
        public GaussDBDataSource? DataSource { get; set; }

        /// <summary>
        /// 管理员数据库名称
        /// </summary>
        public string AdminDatabaseName = "postgres";

        /// <summary>
        /// 启动时探测数据库是否存在的基础等待间隔，默认 1 秒。
        /// <para>实际等待时间会按重试次数指数递增，并受最大等待间隔限制。</para>
        /// </summary>
        public TimeSpan StartupCheckDatabaseExistsBaseDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 单次启动探测允许等待的最大间隔，默认 1 分钟。
        /// <para>用于目标数据库尚未创建完成时限制每次退避等待的上限。</para>
        /// </summary>
        public TimeSpan StartupCheckDatabaseExistsMaxDelay = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 启动时探测数据库是否存在的最大重试次数，默认 5 次。
        /// </summary>
        public int StartupCheckDatabaseExistsMaxRetries = 5;

        /// <summary>
        /// 是否启用：当用用户没有主动设置`NoResetOnClose`时，将会自动设置：`NoResetOnClose` = true;
        /// </summary>
        public bool EnableAutoSetNoResetOnClose = true;

        /// <summary>
        /// 根据配置创建新的 GaussDB 连接。
        /// </summary>
        /// <returns>尚未打开的 GaussDB 连接。</returns>
        internal GaussDBConnection CreateConnection()
        {
            if (DataSource != null) return DataSource.CreateConnection();
            if (ConnectionString == null || string.IsNullOrWhiteSpace(ConnectionString)) throw new ArgumentNullException(nameof(ConnectionString));

            var builder = new GaussDBConnectionStringBuilder(ConnectionString);
            if (EnableAutoSetNoResetOnClose
                && !new Regex(@"No\s+Reset\s+On\s+Close", RegexOptions.IgnoreCase).IsMatch(ConnectionString)
                && !ConnectionString.Contains("NoResetOnClose", StringComparison.OrdinalIgnoreCase))
            {// 用户没有设置，则默认设置
                builder.NoResetOnClose = true;
            }

            return new GaussDBConnection(builder.ToString());
        }
    }

    /// <summary>
    /// 当用户通过 EF Core 配置 GaussDB 存储时，从 DbContext 的 provider options 中补全连接配置。
    /// </summary>
    internal sealed class ConfigureGaussDBOptions : IConfigureOptions<GaussDBOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// 初始化 EF Core 配置适配器，通过服务定位从 DbContext 中解析 GaussDB 连接信息。
        /// </summary>
        /// <param name="serviceScopeFactory">创建 DI 作用域的工厂，用于解析 DbContext 实例。</param>
        public ConfigureGaussDBOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(GaussDBOptions options)
        {
            if (options.DbContextType == null) return;

            // DbContext 依赖 ICapPublisher 会造成启动阶段循环依赖，必须提前阻止。
            if (Helper.IsUsingType<ICapPublisher>(options.DbContextType))
            {
                throw new InvalidOperationException(
                    "We detected that you are using ICapPublisher in DbContext, please change the configuration to use the storage extension directly to avoid circular references! eg: x.UseGaussDB()");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(options.DbContextType);
            var coreOptions = dbContext.GetService<IDbContextOptions>();
            var extension = coreOptions.Extensions.First(x => x.Info.IsDatabaseProvider);

            // GaussDB EF Core provider 同时可能暴露 DataSource 或 ConnectionString，这里通过反射保持生产包无 EF provider 编译依赖。
            options.DataSource = extension.GetType().GetProperty(nameof(options.DataSource))?.GetValue(extension)
                as GaussDBDataSource;
            if (options.DataSource == null)
            {
                options.ConnectionString = extension.GetType().GetProperty(nameof(options.ConnectionString))
                    ?.GetValue(extension) as string;
            }
        }
    }
}
