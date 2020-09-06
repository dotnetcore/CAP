using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.ZeroMQ.InMemory
{

    public class ZeroMQService : BackgroundService
    {
        private readonly PullSocket xsubSocket;
        private readonly PushSocket xpubSocket;

        private readonly ILogger _logger;

        public ZeroMQService(ILogger<ZeroMQService> logger)
        {
            _logger = logger;
            this.xsubSocket = new PullSocket();
            this.xpubSocket = new PushSocket();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            xpubSocket.Bind("tcp://127.0.0.1:5556");
            xsubSocket.Bind("tcp://127.0.0.1:5557");
            _logger.LogInformation("MQBusService started");

            var proxy = new Proxy(xsubSocket, xpubSocket);
            return Task.Run(proxy.Start);
        }

        public override void Dispose()
        {
            base.Dispose();
            this.xsubSocket?.Dispose();
            this.xpubSocket?.Dispose();
        }
    }

}
