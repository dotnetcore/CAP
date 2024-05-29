// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Transport;

/// <summary>
/// Consumer client factory to create consumer client instance.
/// </summary>
public interface IConsumerClientFactory
{
    /// <summary>
    /// Create a new instance of <see cref="IConsumerClient" />.
    /// </summary>
    /// <param name="groupName">Message group name.</param>
    /// <param name="groupConcurrent">Message consumed concurrent limit.</param>
    IConsumerClient Create(string groupName, byte groupConcurrent);
}