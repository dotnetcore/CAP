using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisCacheLogger : TextWriter
    {
        private readonly ILogger logger;

        public RedisCacheLogger(ILogger logger)
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
