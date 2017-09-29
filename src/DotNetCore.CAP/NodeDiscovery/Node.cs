namespace DotNetCore.CAP.NodeDiscovery
{
    public class Node
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public string Tags { get; set; }
    }
}