// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.SqlServer;
using DotNetCore.CAP.SqlServer.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class SqlServerCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<SqlServerOptions> _configure;

        public SqlServerCapOptionsExtension(Action<SqlServerOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();

            services.AddSingleton<DiagnosticProcessorObserver>();
            services.AddSingleton<IStorage, SqlServerStorage>();
            services.AddSingleton<IStorageConnection, SqlServerStorageConnection>();
            services.AddSingleton<ICapPublisher, SqlServerPublisher>();
            services.AddSingleton<ICallbackPublisher>(x => (SqlServerPublisher)x.GetService<ICapPublisher>());
            services.AddSingleton<ICollectProcessor, SqlServerCollectProcessor>();

            services.AddTransient<CapTransactionBase, SqlServerCapTransaction>();

            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<SqlServerOptions>, ConfigureSqlServerOptions>();
        }
    }
}