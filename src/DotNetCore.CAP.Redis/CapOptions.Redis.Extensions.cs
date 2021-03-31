using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace DotNetCore.CAP.Redis
{
    public static class CapRedisOptionsExtensions
    {
        public static CapOptions UseRedis(this CapOptions options) =>
            options.UseRedis(_ => { });

        public static CapOptions UseRedis(this CapOptions options, string connection) =>
            options.UseRedis(opt => opt.Configuration = ConfigurationOptions.Parse(connection));


        public static CapOptions UseRedis(this CapOptions options, Action<CapRedisOptions> configure)
        {
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new RedisOptionsExtension(configure));

            return options;
        }
    }
}
