// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Messages;

public class FailedInfo
{
    public IServiceProvider ServiceProvider { get; set; } = default!;

    public MessageType MessageType { get; set; }

    public Message Message { get; set; } = default!;
}