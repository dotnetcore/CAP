using System;
using Microsoft.AspNetCore.Hosting;

namespace Cap.Consistency.Server
{
    public interface IConsumer : IDisposable
    {
        void Start();

        void Start(int count);  

        void Stop();

        IConsistencyTrace Log { get; set; }

        ConsistencyServerOptions ServerOptions { get; set; }

        IApplicationLifetime AppLifetime { get; set; }
    }
}