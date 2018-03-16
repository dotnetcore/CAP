// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IDispatcher
    {
        void EnqueuToPublish(CapPublishedMessage message);

        void EnqueuToExecute(CapReceivedMessage message);
    }
}