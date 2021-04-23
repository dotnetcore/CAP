using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisLogger : TextWriter
    {
        private readonly ILogger logger;

        public RedisLogger(ILogger logger)
        {
            this.logger = logger;
        }
        

        public override void WriteLine(string value)
        {
            logger.LogInformation(value);
        }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
