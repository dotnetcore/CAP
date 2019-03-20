// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.InMemoryStorage;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class InMemoryCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();
            services.AddSingleton<IStorage, InMemoryStorage.InMemoryStorage>();
            services.AddSingleton<IStorageConnection, InMemoryStorageConnection>();

            services.AddSingleton<ICapPublisher, InMemoryPublisher>();
            services.AddSingleton<ICallbackPublisher, InMemoryPublisher>();

            services.AddTransient<ICollectProcessor, InMemoryCollectProcessor>();
            services.AddTransient<CapTransactionBase, InMemoryCapTransaction>();
        }
    }
}