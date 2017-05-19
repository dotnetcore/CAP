using System;
using System.Collections.Generic;
using System.Text;

namespace Cap.Consistency.Attributes
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


        public bool IsOneWay { get; set; }

    }
}
