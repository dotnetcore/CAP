// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace DotNetCore.CAP.Internal;

public class ConsumerExecutedResult
{
    public ConsumerExecutedResult(object? result, string msgId, string? callbackName, IDictionary<string, string?>? callbackHeader)
    {
        Result = result;
        MessageId = msgId;
        CallbackName = callbackName;
        CallbackHeader = callbackHeader;
    }

    public object? Result { get; set; }

    public string MessageId { get; set; }

    public string? CallbackName { get; set; }

    public IDictionary<string, string?>? CallbackHeader { get; set; }
}