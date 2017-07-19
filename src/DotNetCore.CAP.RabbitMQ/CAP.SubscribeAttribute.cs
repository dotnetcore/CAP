using DotNetCore.CAP.Abstractions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name) : base(name)
        {
        }
    }
}