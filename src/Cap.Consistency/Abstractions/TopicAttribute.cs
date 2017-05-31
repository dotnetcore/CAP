using System;
using System.Collections.Generic;
using System.Text;

namespace Cap.Consistency.Abstractions
{

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public abstract class TopicAttribute : Attribute
    {
        readonly string _name;

        public TopicAttribute(string topicName) {
            this._name = topicName;
        }

        public string Name {
            get { return _name; }
        }

        public string GroupOrExchange { get; set; }

        public bool IsOneWay { get; set; }
    }
}
