// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IServiceBusProducerDescriptorFactory
{
    IServiceBusProducerDescriptor CreateProducerForMessage(TransportMessage transportMessage);
}
