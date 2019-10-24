// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Consumer executor
    /// </summary>
    public interface ISubscriberExecutor
    {
        Task<OperateResult> ExecuteAsync(MediumMessage message, CancellationToken cancellationToken = default);

        Task<OperateResult> ExecuteAsync(MediumMessage message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken = default);
    }
}