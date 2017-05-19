using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Cap.Consistency.Extensions;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency
{
    public class QMessageFinder
    {
        public ConcurrentDictionary<string, ConsumerExecutorDescriptor> GetQMessageMethodInfo(params Type[] serviceType) {

            var qMessageTypes = new ConcurrentDictionary<string, ConsumerExecutorDescriptor>();

            foreach (var type in serviceType) {

                foreach (var method in type.GetTypeInfo().DeclaredMethods) {

                    var messageMethodInfo = new ConsumerExecutorDescriptor();

                    if (method.IsPropertyBinding()) {
                        continue;
                    }

                    var qMessageAttr = method.GetCustomAttribute<QMessageAttribute>();
                    if (qMessageAttr == null) {
                        continue;
                    }
                     
                    messageMethodInfo.ImplType = method.DeclaringType;
                    messageMethodInfo.MethodInfo = method;

                    qMessageTypes.AddOrUpdate(qMessageAttr.MessageName, messageMethodInfo, (x, y) => y);
                }
            }

            return qMessageTypes;
        }
    }
}
