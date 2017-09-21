using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class DownstreamUrl
    {
        public DownstreamUrl(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}
