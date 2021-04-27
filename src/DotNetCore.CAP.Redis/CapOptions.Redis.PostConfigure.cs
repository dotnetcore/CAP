using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RedisStreams
{
    class CapRedisOptionsPostConfigure : IPostConfigureOptions<CapRedisOptions>
    {
        public CapRedisOptionsPostConfigure()
        {
        }

        public void PostConfigure(string name, CapRedisOptions options)
        {
            options.Configuration ??= new ConfigurationOptions();

            if (options.StreamEntriesCount == default)
                options.StreamEntriesCount = 10;

            if (options.ConnectionPoolSize == default)
                options.ConnectionPoolSize= 10;

            if (!options.Configuration.EndPoints.Any())
            {
                options.Configuration.EndPoints.Add(IPAddress.Loopback, 0);
                options.Configuration.SetDefaultPorts();
            }
        }
    }
}
