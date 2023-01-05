

using Azure.Messaging.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus
{
    public class AzureServiceBusConsumerCommitInput
    {
        public AzureServiceBusConsumerCommitInput(ProcessMessageEventArgs processMessageEventArgs)
        {
            ProcessMessageArgs = processMessageEventArgs;
        }

        public ProcessMessageEventArgs ProcessMessageArgs { get; set; }

    }
}
