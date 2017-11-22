using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Internal
{
    internal class TopicName : TopicAttribute
    {
        public TopicName(string name) : base(name)
        {
        }
    }
}