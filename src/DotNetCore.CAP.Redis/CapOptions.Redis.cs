using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    public class CapRedisOptions
    {
        /// <summary>
        /// Gets or sets the options of redis connections
        /// </summary>
        public ConfigurationOptions Configuration { get; set; }

        internal string Endpoint { get; set; }

        public uint StreamEntriesCount { get; set; }
        public uint ConnectionPoolSize { get; set; }
    }
}
