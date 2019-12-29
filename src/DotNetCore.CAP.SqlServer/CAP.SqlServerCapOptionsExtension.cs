// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.SqlServer;
using DotNetCore.CAP.SqlServer.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.AddSingleton<IDataStorage, SqlServerDataStorage>();
            services.AddSingleton<IStorageInitializer, SqlServerStorageInitializer>();
            services.AddTransient<ICapTransaction, SqlServerCapTransaction>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, DiagnosticRegister>());

            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<SqlServerOptions>, ConfigureSqlServerOptions>();
        }
    }
}