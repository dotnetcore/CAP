// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class DiscoveryOptions
    {
        public const string DefaultDiscoveryServerHost = "localhost";
        public const int DefaultDiscoveryServerProt = 8500;

        public const string DefaultCurrentNodeHostName = "localhost";
        public const int DefaultCurrentNodePort = 5000;

        public const string DefaultMatchPath = "/cap";

        public DiscoveryOptions()
        {
            DiscoveryServerHostName = DefaultDiscoveryServerHost;
            DiscoveryServerProt = DefaultDiscoveryServerProt;

            CurrentNodeHostName = DefaultCurrentNodeHostName;
            CurrentNodePort = DefaultCurrentNodePort;

            MatchPath = DefaultMatchPath;
        }

        public string DiscoveryServerHostName { get; set; }
        public int DiscoveryServerProt { get; set; }

        public string CurrentNodeHostName { get; set; }
        public int CurrentNodePort { get; set; }

        public int NodeId { get; set; }
        public string NodeName { get; set; }
        public string MatchPath { get; set; }
    }

}