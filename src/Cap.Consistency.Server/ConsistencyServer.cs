using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.Server
{
    public class ConsistencyServer : IServer
    {
        public ConsistencyServer(IApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            if (loggerFactory==null)
            {
                throw  new ArgumentNullException(nameof(loggerFactory));
            }
        }

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            throw new NotImplementedException();
        }

        public IFeatureCollection Features { get; }

        public void Dispose()
        {
            
            throw new NotImplementedException();
        }
    }
}