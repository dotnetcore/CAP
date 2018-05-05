// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public class SucceededState : IState
    {
        public const string StateName = "Succeeded";

        public SucceededState()
        {
            ExpiresAfter = TimeSpan.FromHours(1);
        }

        public SucceededState(int expireAfterSeconds)
        {
            ExpiresAfter = TimeSpan.FromSeconds(expireAfterSeconds);
        }

        public TimeSpan? ExpiresAfter { get; }

        public string Name => StateName;

        public void Apply(CapPublishedMessage message, IStorageTransaction transaction)
        {
        }

        public void Apply(CapReceivedMessage message, IStorageTransaction transaction)
        {
        }
    }
}