using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class CapOptionsExtensions
{
    public static CapOptions UseGoogleSpanner(this CapOptions options, string connectionString)
    {
        return options.UseGoogleSpanner(opt => { opt.ConnectionString = connectionString; });
    }

    public static CapOptions UseGoogleSpanner(this CapOptions options, Action<GoogleSpannerOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        configure += x => x.Version = options.Version;

        options.RegisterExtension(new GoogleSpannerCapOptionsExtension(configure));

        return options;
    }

    public static CapOptions UseEntityFramework<TContext>(this CapOptions options)
        where TContext : DbContext
    {
        return options.UseEntityFramework<TContext>(opt => { });
    }

    public static CapOptions UseEntityFramework<TContext>(this CapOptions options, Action<EFOptions> configure)
        where TContext : DbContext
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        options.RegisterExtension(new GoogleSpannerCapOptionsExtension(x =>
        {
            configure(x);
            x.Version = options.Version;
            x.DbContextType = typeof(TContext);
        }));

        return options;
    }
}

