// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseEntityFramework<TContext>(this CapOptions options)
            where TContext : DbContext
        {
            return options.UseEntityFramework<TContext>(opt => { });
        }

        public static CapOptions UseEntityFramework<TContext>(this CapOptions options, Action<EFOptions> configure)
            where TContext : DbContext
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options.UseSqlServer(x =>
            {
                configure(x);

                var type = x.GetType();
                var dbContextTypeProperty =
                    type.GetProperty("DbContextType", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new ArgumentException("Can not get \"DbContextType\" property from EFOptions");
                dbContextTypeProperty.SetValue(x, typeof(TContext));
            });

            options.RegisterExtension(new SqlServerCapOptionsExtension());

            return options;
        }
    }
}
