using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using DotNetCore.CAP.RedisStreams;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapRedisOptionsExtensions
    {
        public static CapOptions UseRedis(this CapOptions options) =>
            options.UseRedis(_ => { });

        public static CapOptions UseRedis(this CapOptions options, string connection) =>
            options.UseRedis(opt => opt.Configuration = ConfigurationOptions.Parse(connection));


        public static CapOptions UseRedis(this CapOptions options, Action<CapRedisOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new RedisOptionsExtension(configure));

            return options;
        }
    }
}
