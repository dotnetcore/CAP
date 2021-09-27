// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace DotNetCore.CAP.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// A process thread abstract of message process.
    /// </summary>
    public interface IProcessingServer : IDisposable
    {
        void Pulse() { }

        void Start(CancellationToken stoppingToken);
    }
}