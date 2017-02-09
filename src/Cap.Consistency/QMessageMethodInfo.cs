using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Cap.Consistency
{
    public class QMessageMethodInfo
    {
        public MethodInfo MethodInfo { get; set; }

        public Type ImplType { get; set; }

        public string MessageName { get; set; }
    }
}
