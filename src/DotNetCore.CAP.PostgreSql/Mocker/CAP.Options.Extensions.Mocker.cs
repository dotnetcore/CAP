// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensionsMocker
    {
        public static CapOptions UseMockPostgreSql(this CapOptions options)
        {
            options.RegisterExtension(new MockPostgreSqlCapOptionsExtension());

            return options;
        }


        public static CapOptions UseMockEntityFramework(this CapOptions options)
        {
            options.RegisterExtension(new MockPostgreSqlCapOptionsExtension());

            return options;
        }
    }

    internal class MockPostgreSqlCapOptionsExtension : ICapOptionsExtension
    {
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton(new PostgreSqlOptions());
        }
    }
}