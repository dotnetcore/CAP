using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Cap.Consistency.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cap.Consistency
{
    public class QMessageFinder
    {

        public ConcurrentDictionary<string, QMessageMethodInfo> GetQMessageMethods(IServiceCollection serviceColloection) {

            if (serviceColloection == null) {
                throw new ArgumentNullException(nameof(serviceColloection));
            }

            var qMessageTypes = new ConcurrentDictionary<string, QMessageMethodInfo>();

            foreach (var serviceDescriptor in serviceColloection) {

                foreach (var method in serviceDescriptor.ServiceType.GetTypeInfo().DeclaredMethods) {

                    var messageMethodInfo = new QMessageMethodInfo();

                    if (method.IsPropertyBinding()) {
                        continue;
                    }

                    var qMessageAttr = method.GetCustomAttribute<QMessageAttribute>();
                    if (qMessageAttr == null) {
                        continue;
                    }

                    messageMethodInfo.MessageName = qMessageAttr.MessageName;
                    messageMethodInfo.ImplType = method.DeclaringType;
                    messageMethodInfo.MethodInfo = method;

                    qMessageTypes.AddOrUpdate(qMessageAttr.MessageName, messageMethodInfo, (x, y) => y);
                }
            }
            return qMessageTypes;
        } 
    }
}
