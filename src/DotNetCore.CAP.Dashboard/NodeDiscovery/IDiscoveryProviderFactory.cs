// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard.NodeDiscovery
{
    internal interface IDiscoveryProviderFactory
    {
        INodeDiscoveryProvider Create(DiscoveryOptions options);
    }
}