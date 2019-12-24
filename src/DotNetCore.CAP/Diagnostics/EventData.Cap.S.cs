// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using JetBrains.Annotations;

namespace DotNetCore.CAP.Diagnostics
{
    public class CapEventDataSubStore
    {
        public long? OperationTimestamp { get; set; }

        public string Operation { get; set; }

        public TransportMessage TransportMessage { get; set; }

        public BrokerAddress BrokerAddress { get; set; }

        public long? ElapsedTimeMs { get; set; }

        public Exception Exception { get; set; }
    }

    public class CapEventDataSubExecute
    {
        public long? OperationTimestamp { get; set; }

        public string Operation { get; set; }

        public Message Message { get; set; }

        [CanBeNull]
        public MethodInfo MethodInfo { get; set; }

        public long? ElapsedTimeMs { get; set; }

        public Exception Exception { get; set; }
    }
}
