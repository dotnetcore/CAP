using System;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// An abstract attribute that for  kafka attribute or rabbitmq attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public abstract class TopicAttribute : Attribute
    {
        private readonly string _name;

        public TopicAttribute(string topicName)
        {
            this._name = topicName;
        }

        /// <summary>
        /// topic or exchange route key name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// the consumer group.
        /// </summary>
        public string GroupOrExchange { get; set; }

        public bool IsOneWay { get; set; }
    }
}