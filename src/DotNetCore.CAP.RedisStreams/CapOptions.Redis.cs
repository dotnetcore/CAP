using System;
using StackExchange.Redis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public class CapRedisOptions
    {
        /// <summary>
        /// Gets or sets the options of redis connections
        /// </summary>
        public ConfigurationOptions Configuration { get; set; }

        internal string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the count of entries consumed from stream
        /// </summary>
        public uint StreamEntriesCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of connections that can be used with redis server
        /// </summary>
        public uint ConnectionPoolSize { get; set; }
    }
}
