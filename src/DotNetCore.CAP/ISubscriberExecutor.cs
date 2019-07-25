// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Consumer executor
    /// </summary>
    public interface ISubscriberExecutor
    {
        Task<OperateResult> ExecuteAsync(CapReceivedMessage message, CancellationToken cancellationToken = default);
    }
}