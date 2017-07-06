using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public abstract class TestHost : IDisposable
    {
        protected IServiceCollection _services;
        private IServiceProvider _provider;
        private IServiceProvider _scopedProvider;

        public TestHost()
        {
            CreateServiceCollection();
            PreBuildServices();
            BuildServices();
            PostBuildServices();
        }

        protected IServiceProvider Provider => _scopedProvider ?? _provider;

        private void CreateServiceCollection()
        {
            var services = new ServiceCollection();

            services.AddOptions();
            services.AddLogging();

            var connectionString = ConnectionUtil.GetConnectionString();
            //services.AddSingleton(new SqlServerOptions { ConnectionString = connectionString });
            services.AddDbContext<TestDbContext>(options => options.UseSqlServer(connectionString));

            _services = services;
        }

        protected virtual void PreBuildServices()
        {
        }

        private void BuildServices()
        {
            _provider = _services.BuildServiceProvider();
        }

        protected virtual void PostBuildServices()
        {
        }

        public IDisposable CreateScope()
        {
            var scope = CreateScope(_provider);
            var loc = scope.ServiceProvider;
            _scopedProvider = loc;
            return new DelegateDisposable(() =>
            {
                if (_scopedProvider == loc)
                {
                    _scopedProvider = null;
                }
                scope.Dispose();
            });
        }

        public IServiceScope CreateScope(IServiceProvider provider)
        {
            var scope = provider.GetService<IServiceScopeFactory>().CreateScope();
            return scope;
        }

        public T GetService<T>() => Provider.GetService<T>();

        public T Ensure<T>(ref T service)
            where T : class
            => service ?? (service = GetService<T>());

        public virtual void Dispose()
        {
            (_provider as IDisposable)?.Dispose();
        }

        private class DelegateDisposable : IDisposable
        {
            private Action _dispose;

            public DelegateDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }
}