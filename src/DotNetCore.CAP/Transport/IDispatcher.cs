// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Transport
{
    public interface IDispatcher
    {
        void EnqueueToPublish(IMediumMessage message);

        void EnqueueToExecute(IMediumMessage message, ConsumerExecutorDescriptor descriptor);
    }
}