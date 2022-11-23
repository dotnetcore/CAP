// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Internal;

/// <summary>
/// Consumer executor
/// </summary>
public interface ISubscribeExecutor
{
    Task<OperateResult> ExecuteAsync(MediumMessage message, ConsumerExecutorDescriptor? descriptor = null, CancellationToken cancellationToken = default);
}