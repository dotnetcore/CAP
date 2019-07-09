// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.MySql;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class MySqlCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<MySqlOptions> _configure;

        public MySqlCapOptionsExtension(Action<MySqlOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();
            services.AddSingleton<IStorage, MySqlStorage>();
            services.AddSingleton<IStorageConnection, MySqlStorageConnection>();
            services.AddSingleton<ICapPublisher, MySqlPublisher>();
            services.AddSingleton<ICallbackPublisher>(provider => (MySqlPublisher)provider.GetService<ICapPublisher>());
            services.AddSingleton<ICollectProcessor, MySqlCollectProcessor>();

            services.AddTransient<CapTransactionBase, MySqlCapTransaction>();

            //Add MySqlOptions
            services.Configure(_configure);
            services.AddSingleton<IConfigureOptions<MySqlOptions>, ConfigureMySqlOptions>();
        } 
    }
}