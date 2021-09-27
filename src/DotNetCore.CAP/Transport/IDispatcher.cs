// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Transport
{
    public interface IDispatcher : IProcessingServer
    {
        void EnqueueToPublish(MediumMessage message);

        void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor);
    }
}