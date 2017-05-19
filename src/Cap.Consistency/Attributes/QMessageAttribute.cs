using System;

namespace Cap.Consistency
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    sealed class QMessageAttribute : Attribute
    {
        public QMessageAttribute(string messageName) {
            MessageName = messageName;
        }
 
        public string MessageName { get; private set; }
    }
}
