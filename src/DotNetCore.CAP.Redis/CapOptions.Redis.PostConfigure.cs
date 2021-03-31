using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class CapRedisOptionsPostConfigure : IPostConfigureOptions<CapRedisOptions>
    {
        private readonly CapOptions capOptions;

        public CapRedisOptionsPostConfigure(IOptions<CapOptions> options)
        {
            capOptions = options.Value;
        }

        public void PostConfigure(string name, CapRedisOptions options)
        {
            var groupPrefix = string.IsNullOrWhiteSpace(capOptions.GroupNamePrefix) ? string.Empty : $"{capOptions.GroupNamePrefix}.";
            
            options.DefaultChannel = $"{groupPrefix}{capOptions.DefaultGroupName}";

            options.Configuration ??= new ConfigurationOptions();

            if (!options.Configuration.EndPoints.Any())
            {
                options.Configuration.EndPoints.Add(IPAddress.Loopback, 0);
                options.Configuration.SetDefaultPorts();
            }
        }
    }
}
