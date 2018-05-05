// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents a persisted storage.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Initializes the storage. For example, making sure a database is created and migrations are applied.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Provider the dashboard metric api.
        /// </summary>
        IMonitoringApi GetMonitoringApi();

        /// <summary>
        /// Storage connection of database operate.
        /// </summary>
        IStorageConnection GetConnection();
    }
}