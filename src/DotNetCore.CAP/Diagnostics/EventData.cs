// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class EventData
    {
        public EventData(Guid operationId, string operation)
        {
            OperationId = operationId;
            Operation = operation;
        }

        public Guid OperationId { get; set; }

        public string Operation { get; set; }
    }
}