// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.MongoDB;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensionsMocker
    {
        public static CapOptions UseMockMongoDB(this CapOptions options)
        {
            options.RegisterExtension(new MockMongoDBCapOptionsExtension());

            return options;
        }
    }

    internal class MockMongoDBCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<IMongoTransaction, NullMongoTransaction>();
        }
    }
}