// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public interface IState
    {
        TimeSpan? ExpiresAfter { get; }

        string Name { get; }

        void Apply(CapPublishedMessage message, IStorageTransaction transaction);

        void Apply(CapReceivedMessage message, IStorageTransaction transaction);
    }
}