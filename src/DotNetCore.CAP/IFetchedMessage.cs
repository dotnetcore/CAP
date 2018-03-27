// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IFetchedMessage : IDisposable
    {
        int MessageId { get; }

        MessageType MessageType { get; }

        void RemoveFromQueue();

        void Requeue();
    }
}