// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.InMemoryStorage;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class InMemoryCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();

            services.AddTransient<ICapTransaction, InMemoryCapTransaction>();
            services.AddSingleton<IDataStorage, InMemoryStorage.InMemoryStorage>();
            services.AddSingleton<IStorageInitializer, InMemoryStorageInitializer>();
        }
    }
}