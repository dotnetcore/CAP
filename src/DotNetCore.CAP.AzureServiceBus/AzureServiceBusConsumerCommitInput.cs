using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus
{
    public class AzureServiceBusConsumerCommitInput
    {
        public AzureServiceBusConsumerCommitInput(string lockToken, string destinationTopicPath)
        {
            LockToken = lockToken;
            DestinationTopicPath = destinationTopicPath;
        }
        
        public AzureServiceBusConsumerCommitInput(string lockToken, IMessageSession? session, string destinationTopicPath)
        {
            LockToken = lockToken;
            Session = session;
            DestinationTopicPath = destinationTopicPath;
        }
        
        public IMessageSession? Session { get; set; }
        
        /// <summary>
        /// The LockToken used for message Completion
        /// </summary>
        public string LockToken { get; set; }
        
        /// <summary>
        /// Stores the value of the Destination header from <see cref="CAP.Messages.Headers"/>, that translates to which Service Bus Topic the message was produced/should be consumed.
        /// </summary>
        public string DestinationTopicPath { get; }
    }
}
