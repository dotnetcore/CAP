// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// CAP transaction wrapper, used to wrap database transactions, provides a consistent user interface
    /// </summary>
    public interface ICapTransaction : IDisposable
    {
        /// <summary>
        /// A flag is used to indicate whether the transaction is automatically committed after the message is published
        /// </summary>
        bool AutoCommit { get; set; }

        /// <summary>
        /// Database transaction object, can be converted to a specific database transaction object or IDBTransaction when used
        /// </summary>
        object? DbTransaction { get; set; }

        /// <summary>
        /// Submit the transaction context of the CAP, we will send the message to the message queue at the time of submission
        /// </summary>
        void Commit();

        /// <summary>
        /// Submit the transaction context of the CAP, we will send the message to the message queue at the time of submission
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// We will delete the message data that has not been store in the buffer data of current transaction context.
        /// </summary>
        void Rollback();

        /// <summary>
        /// We will delete the message data that has not been store in the buffer data of current transaction context.
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}