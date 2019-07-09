// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.PostgreSql;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class PostgreSqlCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<PostgreSqlOptions> _configure;

        public PostgreSqlCapOptionsExtension(Action<PostgreSqlOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();
            services.AddSingleton<IStorage, PostgreSqlStorage>();
            services.AddSingleton<IStorageConnection, PostgreSqlStorageConnection>();
            services.AddSingleton<ICapPublisher, PostgreSqlPublisher>();
            services.AddSingleton<ICallbackPublisher>(provider => (PostgreSqlPublisher)provider.GetService<ICapPublisher>());
            services.AddSingleton<ICollectProcessor, PostgreSqlCollectProcessor>();

            services.AddTransient<CapTransactionBase, PostgreSqlCapTransaction>();

            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<PostgreSqlOptions>, ConfigurePostgreSqlOptions>();
        }
    }
}