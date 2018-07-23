using System;
using DotNetCore.CAP;
using DotNetCore.CAP.MongoDB;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseMongoDB(this CapOptions options)
        {
            return options.UseMongoDB(x => { });
        }

        public static CapOptions UseMongoDB(this CapOptions options, Action<MongoDBOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new MongoDBCapOptionsExtension(configure));

            return options;
        }
    }
}