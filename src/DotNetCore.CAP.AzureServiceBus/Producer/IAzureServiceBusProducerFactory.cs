using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IAzureServiceBusProducerFactory
{
    IAzureServiceBusProducer CreateProducerForMessage(TransportMessage transportMessage);
}