// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace DotNetCore.CAP.Transport;

/// <summary>
/// Factory interface for creating instances of <see cref="IConsumerClient"/>.
/// </summary>
public interface IConsumerClientFactory
{
    /// <summary>
    /// Asynchronously creates a new <see cref="IConsumerClient"/> instance.
    /// </summary>
    /// <param name="groupName">The name of the message group.</param>
    /// <param name="groupConcurrent">The maximum number of concurrent messages to consume.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="IConsumerClient"/> instance.</returns>
    Task<IConsumerClient> CreateAsync(string groupName, byte groupConcurrent);
}
