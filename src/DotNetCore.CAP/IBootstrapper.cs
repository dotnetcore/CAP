// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP;

/// <summary>
/// Defines the contract for CAP bootstrapping logic that initializes the system when the application starts.
/// Implementations perform setup tasks such as initializing storage, registering consumers, or preparing the message queue.
/// </summary>
/// <remarks>
/// The bootstrapper is responsible for:
/// <list type="bullet">
/// <item><description>Initializing storage tables or schema if not already present.</description></item>
/// <item><description>Registering consumer subscribers from discovered assemblies.</description></item>
/// <item><description>Verifying connection to message brokers and storage backends.</description></item>
/// <item><description>Preparing the system for message publishing and consuming operations.</description></item>
/// </list>
/// The bootstrapper is registered as a hosted service and automatically starts when the application starts.
/// </remarks>
public interface IBootstrapper : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the bootstrap process has completed successfully.
    /// Returns true if the system is fully initialized; false if bootstrap is still in progress or failed.
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// Asynchronously performs the bootstrap initialization for CAP.
    /// This method is called when the application starts and should complete all necessary initialization.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous bootstrap operation.</returns>
    Task BootstrapAsync(CancellationToken cancellationToken = default);
}