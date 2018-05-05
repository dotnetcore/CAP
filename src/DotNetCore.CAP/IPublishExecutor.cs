// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// publish message excutor. The excutor sends the message to the message queue
    /// </summary>
    public interface IPublishExecutor
    {
        /// <summary>
        /// publish message to message queue.
        /// </summary>
        /// <param name="keyName">The message topic name.</param>
        /// <param name="content">The message content.</param>
        /// <returns></returns>
        Task<OperateResult> PublishAsync(string keyName, string content);
    }
}