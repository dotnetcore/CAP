// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP;

/// <summary>
/// Represents bootstrapping logic. For example, adding initial state to the storage or querying certain entities.
/// </summary>
public interface IBootstrapper : IAsyncDisposable
{
    bool IsStarted { get; }

    Task BootstrapAsync(CancellationToken cancellationToken = default);
}