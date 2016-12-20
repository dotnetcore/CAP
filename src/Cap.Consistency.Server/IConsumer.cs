using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cap.Consistency.Server
{
    public interface IConsumer : IDisposable
    {
        void Start();

        void Start(int count);

        void Stop();

        ILogger Log { get; set; }

        ConsistencyServerOptions ServerOptions { get; set; }

        IApplicationLifetime AppLifetime { get; set; }
    }
}
