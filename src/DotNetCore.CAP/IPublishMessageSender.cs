// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IPublishMessageSender
    {
        Task<OperateResult> SendAsync(CapPublishedMessage message);
    }
}