// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;

namespace DotNetCore.CAP.MySql;

internal class ServerVersion
{
    public enum ServerType
    {
        MySql = 0,
        MariaDb = 1
    }

    private static readonly Regex VersionRegex = new("\\d+\\.\\d+\\.?(?:\\d+)?");

    public ServerVersion(ServerType serverType, Version version)
    {
        Type = serverType;
        Version = version;
    }

    public Version Version { get; }

    public ServerType Type { get; }

    public static ServerVersion? Parse(string versionString)
    {
        var mariaDbTypeIdentifier = "MariaDb".ToLowerInvariant();
        var matchCollection = VersionRegex.Matches(versionString);
        if (matchCollection.Count > 0)
        {
            var serverType = (ServerType)(versionString.ToLower().Contains(mariaDbTypeIdentifier) ? 1 : 0);
            var version = serverType != ServerType.MariaDb || matchCollection.Count <= 1
                ? Version.Parse(matchCollection[0].Value)
                : Version.Parse(matchCollection[1].Value);
            return new ServerVersion(serverType, version);
        }

        return null;
    }
}