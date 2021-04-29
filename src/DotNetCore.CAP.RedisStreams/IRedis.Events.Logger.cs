// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisLogger : TextWriter
    {
        private readonly ILogger logger;

        public RedisLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public override Encoding Encoding => Encoding.UTF8;


        public override void WriteLine(string value)
        {
            logger.LogInformation(value);
        }
    }
}