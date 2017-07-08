namespace DotNetCore.CAP.Infrastructure
{
    public class MessageContext
    {
        public string Group { get; set; }

        public string KeyName { get; set; }

        public string Content { get; set; }
    }
}