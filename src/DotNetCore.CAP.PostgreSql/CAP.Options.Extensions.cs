using System;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UsePostgreSql(this CapOptions options, string connectionString)
        {
            return options.UsePostgreSql(opt => { opt.ConnectionString = connectionString; });
        }

        public static CapOptions UsePostgreSql(this CapOptions options, Action<PostgreSqlOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new PostgreSqlCapOptionsExtension(configure));

            return options;
        }

        public static CapOptions UseEntityFramework<TContext>(this CapOptions options)
            where TContext : DbContext
        {
            return options.UseEntityFramework<TContext>(opt => { opt.DbContextType = typeof(TContext); });
        }

        public static CapOptions UseEntityFramework<TContext>(this CapOptions options, Action<EFOptions> configure)
            where TContext : DbContext
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var efOptions = new EFOptions {DbContextType = typeof(TContext)};
            configure(efOptions);

            options.RegisterExtension(new PostgreSqlCapOptionsExtension(configure));

            return options;
        }
    }
}