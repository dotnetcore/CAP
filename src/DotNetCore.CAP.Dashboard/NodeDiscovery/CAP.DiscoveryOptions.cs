// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    public class DiscoveryOptions
    {
        public const string DefaultDiscoveryServerHost = "localhost";
        public const int DefaultDiscoveryServerPort = 8500;

        public const string DefaultCurrentNodeHostName = "localhost";
        public const int DefaultCurrentNodePort = 5000;

        public const string DefaultMatchPath = "/cap";

        public const string DefaultScheme = "http";

        public DiscoveryOptions()
        {
            DiscoveryServerHostName = DefaultDiscoveryServerHost;
            DiscoveryServerPort = DefaultDiscoveryServerPort;

            CurrentNodeHostName = DefaultCurrentNodeHostName;
            CurrentNodePort = DefaultCurrentNodePort;

            MatchPath = DefaultMatchPath;

            Scheme = DefaultScheme;
        }

        public string DiscoveryServerHostName { get; set; }
        public int DiscoveryServerPort { get; set; }

        public string CurrentNodeHostName { get; set; }
        public int CurrentNodePort { get; set; }

        public string NodeId { get; set; }
        public string NodeName { get; set; }

        public string MatchPath { get; set; }

        public string Scheme { get; set; }

        public string[] CustomTags { get; set; }
    }
}