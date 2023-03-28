// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace DotNetCore.CAP.AmazonSQS
{
    internal class SQSReceivedMessage
    {
        public string? Message { get; set; }

        public Dictionary<string, SQSReceivedMessageAttributes> MessageAttributes { get; set; } = default!;
    }

    internal class SQSReceivedMessageAttributes
    {
        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}
