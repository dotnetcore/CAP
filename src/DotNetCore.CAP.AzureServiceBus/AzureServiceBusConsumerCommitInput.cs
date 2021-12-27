using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus
{
    public class AzureServiceBusConsumerCommitInput
    {
        public AzureServiceBusConsumerCommitInput(string lockToken, IMessageSession? session = null)
        {
            LockToken = lockToken;
            Session = session;
        }
        
        public IMessageSession? Session { get; set; }
        public string LockToken { get; set; }
    }
}
