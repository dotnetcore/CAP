// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard.NodeDiscovery;

public class ConsulDiscoveryOptions
{
    public const string DefaultDiscoveryServerHost = "localhost";
    public const int DefaultDiscoveryServerPort = 8500;

    public const string DefaultCurrentNodeHostName = "localhost";
    public const int DefaultCurrentNodePort = 5000;

    public const string DefaultMatchPath = "/cap";

    public const string DefaultScheme = "http";

    public string DiscoveryServerHostName { get; set; } = DefaultDiscoveryServerHost;
    public int DiscoveryServerPort { get; set; } = DefaultDiscoveryServerPort;

    public string CurrentNodeHostName { get; set; } = DefaultCurrentNodeHostName;
    public int CurrentNodePort { get; set; } = DefaultCurrentNodePort;

    public string NodeId { get; set; }
    public string NodeName { get; set; }

    public string MatchPath { get; set; } = DefaultMatchPath;

    public string Scheme { get; set; } = DefaultScheme;

    public string[] CustomTags { get; set; }
}