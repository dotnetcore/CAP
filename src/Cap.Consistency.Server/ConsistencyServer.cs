using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Reflection;
using Cap.Consistency.Server.Internal.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cap.Consistency.Server
{
    public class ConsistencyServer : IServer
    {
        private Stack<IDisposable> _disposables;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;
        private readonly IServerAddressesFeature _serverAddresses;
        private readonly IConsumer _consumer;

        public ConsistencyServer(IOptions<ConsistencyServerOptions> options, IApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            if (applicationLifetime == null) {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            if (loggerFactory == null) {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Options = options.Value ?? new ConsistencyServerOptions();
            _applicationLifetime = applicationLifetime;
            _logger = loggerFactory.CreateLogger(typeof(ConsistencyServer).GetTypeInfo().Namespace);
            _consumer = Options.ApplicationServices.GetService<IConsumer>();
            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set(_serverAddresses);
        }

        public IFeatureCollection Features { get; }

        public ConsistencyServerOptions Options { get; }

        public void Start<TContext>(IHttpApplication<TContext> application) {
            if (_disposables != null) {
                // The server has already started and/or has not been cleaned up yet
                throw new InvalidOperationException("Server has already started.");
            }
            _disposables = new Stack<IDisposable>();
            var trace = new ConsistencyTrace(_logger);

            _disposables.Push(_consumer);

            var threadCount = Options.ThreadCount;

            if (threadCount <= 0) {
                throw new ArgumentOutOfRangeException(nameof(threadCount),
                    threadCount,
                    "ThreadCount must be positive.");
            }

            _consumer.Start();
        }

        public void Dispose() {
            if (_disposables != null) {
                while (_disposables.Count > 0) {
                    _disposables.Pop().Dispose();
                }
                _disposables = null;
            }
        }
    }
}