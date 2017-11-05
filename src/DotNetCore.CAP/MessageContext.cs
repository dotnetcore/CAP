namespace DotNetCore.CAP
{
    /// <summary>
    /// Message context
    /// </summary>
    public class MessageContext
    {
        /// <summary>
        /// Gets or sets the message group.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Message name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; }

        public override string ToString()
        {
            return $"Group:{Group}, Name:{Name}, Content:{Content}";
        }
    }
}