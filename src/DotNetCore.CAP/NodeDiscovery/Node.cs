using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.NodeDiscovery
{
    class Node
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public string Tags { get; set; }
    }
}
