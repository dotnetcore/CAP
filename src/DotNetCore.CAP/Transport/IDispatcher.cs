// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Transport;

public interface IDispatcher : IProcessingServer
{
    Task EnqueueToPublish(MediumMessage message);

    Task EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor);
}