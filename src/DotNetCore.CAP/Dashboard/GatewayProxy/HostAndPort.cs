using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class HostAndPort
    {
        public HostAndPort(string downstreamHost, int downstreamPort)
        {
            DownstreamHost = downstreamHost?.Trim('/');
            DownstreamPort = downstreamPort;
        }

        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
    }
}
