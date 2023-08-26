// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard.NodeDiscovery;

public interface INodeDiscoveryProvider
{
    Task<IList<Node>> GetNodes(string ns = null, CancellationToken cancellationToken = default);

    Task<Node> GetNode(string nodeName, string ns = null, CancellationToken cancellationToken = default);

    Task RegisterNode(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    Task<List<string>> GetNamespaces(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<string>());
    }

    Task<IList<Node>> ListServices(string ns = null)
    {
        throw new NotImplementedException();
    }
}