using System;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// An abstract attribute that for  kafka attribute or rabbitmq attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public abstract class TopicAttribute : Attribute
    {
        protected TopicAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// topic or exchange route key name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// kafak --> groups.id
        /// rabbitmq --> queue.name
        /// </summary>
        public string Group { get; set; } = "cap.default.group";

        /// <summary>
        /// unused now
        /// </summary>
        public bool IsOneWay { get; set; }
    }
}