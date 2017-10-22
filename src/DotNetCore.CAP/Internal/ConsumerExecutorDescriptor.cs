using System.Reflection;
using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// A descriptor of user definition method.
    /// </summary>
    internal class ConsumerExecutorDescriptor
    {
        public MethodInfo MethodInfo { get; set; }

        public TypeInfo ImplTypeInfo { get; set; }

        public TopicAttribute Attribute { get; set; }
    }
}