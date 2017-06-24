namespace DotNetCore.CAP.Infrastructure
{
    public class CapReceivedMessage : CapMessage
    {
        public CapReceivedMessage(MessageBase baseMessage) : base(baseMessage)
        {
        }
    }
}