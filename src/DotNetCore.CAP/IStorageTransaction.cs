// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A transactional database storage operation.
    /// Update message state of the message table with transactional.
    /// </summary>
    public interface IStorageTransaction : IDisposable
    {
        void UpdateMessage(CapPublishedMessage message);

        void UpdateMessage(CapReceivedMessage message);

        Task CommitAsync();
    }
}