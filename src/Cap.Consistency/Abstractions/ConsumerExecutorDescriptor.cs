using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cap.Consistency.Abstractions
{
    public class ConsumerExecutorDescriptor
    {
        public MethodInfo MethodInfo { get; set; }

        public TypeInfo ImplTypeInfo { get; set; }

        public TopicInfo Topic { get; set; }

        public string GroupId { get; set; }
    }
}
