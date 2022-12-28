

namespace DotNetCore.CAP.AzureServiceBus
{
    public class AzureServiceBusConsumerCommitInput
    {
        public AzureServiceBusConsumerCommitInput(string lockToken)
        {
            LockToken = lockToken;
        }
        
        public string LockToken { get; set; }
    }
}
