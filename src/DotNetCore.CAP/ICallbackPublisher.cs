// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A callback that is sent to Producer after a successful consumer execution
    /// </summary>
    public interface ICallbackPublisher
    {
        /// <summary>
        /// Publish a callback message
        /// </summary>
        Task PublishCallbackAsync(CapPublishedMessage obj);
    }
}