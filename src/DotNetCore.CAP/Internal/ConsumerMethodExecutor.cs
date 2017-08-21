using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace DotNetCore.CAP.Internal
{
    public class ConsumerMethodExecutor
    {
        internal static object[] PrepareArguments(
            IDictionary<string, object> actionParameters,
            ObjectMethodExecutor actionMethodExecutor)
        {
            var declaredParameterInfos = actionMethodExecutor.MethodParameters;
            var count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

            var arguments = new object[count];
            for (var index = 0; index < count; index++)
            {
                var parameterInfo = declaredParameterInfos[index];
                object value;

                if (!actionParameters.TryGetValue(parameterInfo.Name, out value))
                {
                    value = actionMethodExecutor.GetDefaultValueForParameter(index);
                }

                arguments[index] = value;
            }

            return arguments;
        }
    }
}