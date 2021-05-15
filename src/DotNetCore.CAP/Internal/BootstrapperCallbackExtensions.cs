using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace DotNetCore.CAP.Internal
{
    public static  class BootstrapperCallbackExtensions
    {
        internal static void CallInternalBootStrappingMethod(IEnumerable<IBootstrapperCallback> bootstrapperCallbacks, object[] parameters=null,[CallerMemberName] string callerName="")
        {
            if (string.IsNullOrEmpty(callerName))
            {
                return;
            }

            foreach (var bootStrapperCallbackItem in bootstrapperCallbacks)
            {
                var methods = bootStrapperCallbackItem.GetType().GetMethods();

                var callbackMethod = methods?.FirstOrDefault(o => o.Name.ToLower().Contains(callerName.ToLower()));

                if (callbackMethod == null)
                {
                    return;
                }

                callbackMethod.Invoke(bootStrapperCallbackItem, parameters);
            }
        }

        /// <summary>
        /// Invokes BootStrappingStarted method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        internal static void BootStrappingStarted(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks);
        }

        /// <summary>
        /// Invokes BootStrappingSuccess method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        internal static void OnStart(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks);
        }

        /// <summary>
        /// Invokes StorageInitStarted method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        internal static void StorageInitStarted(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks);
        }

        /// <summary>
        /// Invokes StorageInitFailed method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        /// <param name="exception">Exception instance which happened during StorageFaliure</param>
        internal static void StorageInitFailed(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks,Exception exception)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks,new[] { exception });
        }

        /// <summary>
        /// Invokes StorageInitSuccess method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        internal static void StorageInitSuccess(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks);
        }

        /// <summary>
        /// Invokes BootStrappingFailed method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        /// <param name="exception">Exception instance which happened during Bootstrapping Faliure</param>
        internal static void BootStrappingFailed(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks,Exception exception)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks, new[] { exception });
        }

        /// <summary>
        /// Invokes OnStop method in each callback instance which are registered
        /// </summary>
        /// <param name="bootStrappingCallbacks"></param>
        internal static void OnStop(this IEnumerable<IBootstrapperCallback> bootStrappingCallbacks)
        {
            CallInternalBootStrappingMethod(bootStrappingCallbacks);
        }
    }
}
