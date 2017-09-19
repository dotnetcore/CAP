using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    public class NodeConfiguration
    {
        public string ServerHostName { get; set; }

        public int ServerProt { get; set; }

        public int CurrentPort { get; set; }

        public string PathMatch { get; set; } = "/cap";
    }
}
