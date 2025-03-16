using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CAPOptionsExtensions
    {
        public static CapOptions UseDM(this CapOptions options, string connectionString)
        {
            return options.UseDM(opt => { opt.ConnectionString = connectionString; });
        }

        public static CapOptions UseDM(this CapOptions options, Action<DMOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            configure += x => x.Version = options.Version;

            options.RegisterExtension(new DMCapOptionsExtension(configure));

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

            options.RegisterExtension(new DMCapOptionsExtension(x =>
            {
                configure(x);
                x.Version = options.Version;
                x.DbContextType = typeof(TContext);
            }));

            return options;
        }
    }
}
