using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IServiceBusProducerDescriptorFactory
{
    IServiceBusProducerDescriptor CreateProducerForMessage(TransportMessage transportMessage);
}