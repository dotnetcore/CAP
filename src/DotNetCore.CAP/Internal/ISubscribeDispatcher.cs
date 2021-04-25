// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Consumer executor
    /// </summary>
    public interface ISubscribeDispatcher
    {
        Task<OperateResult> DispatchAsync(IMediumMessage message, CancellationToken cancellationToken = default);

        Task<OperateResult> DispatchAsync(IMediumMessage message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken = default);

        Task<OperateResult> DispatchAsync<T>(IMediumMessage message, CancellationToken cancellationToken = default)
            where T : new();

        Task<OperateResult> DispatchAsync<T>(IMediumMessage message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken = default)
            where T : new();
    }
}