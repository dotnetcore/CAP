namespace DotNetCore.CAP
{
    public class MessageContext
    {
        public string Group { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public override string ToString()
        {
            return $"Group:{Group}, Name:{Name}, Content:{Content}";
        }
    }
}