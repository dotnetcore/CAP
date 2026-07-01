using System;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.GaussDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    /// <summary>
    /// CAP 的 GaussDB 存储扩展注册入口，负责把存储初始化器、消息存储和配置绑定到 DI 容器。
    /// </summary>
    internal sealed class GaussDBCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<GaussDBOptions> _configure;

        /// <summary>
        /// 初始化 GaussDB 存储扩展，绑定用户对 <see cref="GaussDBOptions"/> 的自定义配置。
        /// </summary>
        /// <param name="configure">用户提供的配置委托。</param>
        public GaussDBCapOptionsExtension(Action<GaussDBOptions> configure)
        {
            _configure = configure;
        }
        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="services"></param>
        public void AddServices(IServiceCollection services)
        {
            // 存储标记用于 CAP 内部识别当前启用的持久化实现。
            services.AddSingleton(new CapStorageMarkerService("GaussDB"));
            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<GaussDBOptions>, ConfigureGaussDBOptions>();
            services.AddSingleton<IStorageInitializer, GaussDBStorageInitializer>();
            services.AddSingleton<IDataStorage, GaussDBDataStorage>();
        }
    }
}
