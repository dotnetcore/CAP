// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Internal
{
    public class ConsumerExecutedResult
    {
        public ConsumerExecutedResult(object? result, string msgId, string? callbackName)
        {
            Result = result;
            MessageId = msgId;
            CallbackName = callbackName;
        }

        public object? Result { get; set; }

        public string MessageId { get; set; }

        public string? CallbackName { get; set; }
    }
}