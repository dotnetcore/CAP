using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Internal
{
    public class ObjectMethodExecutor
    {
        private readonly object[] _parameterDefaultValues;
        private readonly ConsumerMethodExecutorAsync _executorAsync;
        private readonly ConsumerMethodExecutor _executor;

        private static readonly MethodInfo _convertOfTMethod =
            typeof(ObjectMethodExecutor).GetRuntimeMethods()
                .Single(methodInfo => methodInfo.Name == nameof(Convert));

        private ObjectMethodExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            TargetTypeInfo = targetTypeInfo;
            MethodParameters = methodInfo.GetParameters();
            MethodReturnType = methodInfo.ReturnType;
            IsMethodAsync = typeof(Task).IsAssignableFrom(MethodReturnType);
            TaskGenericType = IsMethodAsync ? GetTaskInnerTypeOrNull(MethodReturnType) : null;

            if (IsMethodAsync && TaskGenericType != null)
            {
                _executor = GetExecutor(methodInfo, targetTypeInfo);
                _executorAsync = GetExecutorAsync(TaskGenericType, methodInfo, targetTypeInfo);
            }
            else
            {
                _executor = GetExecutor(methodInfo, targetTypeInfo);
            }

            _parameterDefaultValues = GetParameterDefaultValues(MethodParameters);
        }

        private delegate Task<object> ConsumerMethodExecutorAsync(object target, object[] parameters);

        private delegate object ConsumerMethodExecutor(object target, object[] parameters);

        private delegate void VoidActionExecutor(object target, object[] parameters);

        public MethodInfo MethodInfo { get; }

        public ParameterInfo[] MethodParameters { get; }

        public TypeInfo TargetTypeInfo { get; }

        public Type TaskGenericType { get; }

        // This field is made internal set because it is set in unit tests.
        public Type MethodReturnType { get; internal set; }

        public bool IsMethodAsync { get; }

        //public bool IsTypeAssignableFromIActionResult { get; }

        public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            var executor = new ObjectMethodExecutor(methodInfo, targetTypeInfo);
            return executor;
        }

        public Task<object> ExecuteAsync(object target, params object[] parameters)
        {
            return _executorAsync(target, parameters);
        }

        public object Execute(object target, params object[] parameters)
        {
            return _executor(target, parameters);
        }

        public object GetDefaultValueForParameter(int index)
        {
            if (index < 0 || index > MethodParameters.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _parameterDefaultValues[index];
        }

        private static ConsumerMethodExecutor GetExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
            var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

            // methodCall is "((Ttarget) target) method((T0) parameters[0], (T1) parameters[1], ...)"
            // Create function
            if (methodCall.Type == typeof(void))
            {
                var lambda = Expression.Lambda<VoidActionExecutor>(methodCall, targetParameter, parametersParameter);
                var voidExecutor = lambda.Compile();
                return WrapVoidAction(voidExecutor);
            }
            else
            {
                // must coerce methodCall to match ActionExecutor signature
                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambda =
                    Expression.Lambda<ConsumerMethodExecutor>(castMethodCall, targetParameter, parametersParameter);
                return lambda.Compile();
            }
        }

        private static ConsumerMethodExecutor WrapVoidAction(VoidActionExecutor executor)
        {
            return delegate (object target, object[] parameters)
            {
                executor(target, parameters);
                return null;
            };
        }

        private static ConsumerMethodExecutorAsync GetExecutorAsync(
            Type taskInnerType,
            MethodInfo methodInfo,
            TypeInfo targetTypeInfo)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
            var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

            var coerceMethodCall = GetCoerceMethodCallExpression(taskInnerType, methodCall, methodInfo);

            var lambda = Expression.Lambda<ConsumerMethodExecutorAsync>(coerceMethodCall,
                targetParameter, parametersParameter);

            return lambda.Compile();
        }

        // We need to CoerceResult as the object value returned from methodInfo.Invoke has to be cast to a Task<T>.
        // This is necessary to enable calling await on the returned task.
        // i.e we need to write the following var result = await (Task<ActualType>)mInfo.Invoke.
        // Returning Task<object> enables us to await on the result.
        private static Expression GetCoerceMethodCallExpression(
            Type taskValueType,
            MethodCallExpression methodCall,
            MethodInfo methodInfo)
        {
            var castMethodCall = Expression.Convert(methodCall, typeof(object));
            var genericMethodInfo = _convertOfTMethod.MakeGenericMethod(taskValueType);
            var genericMethodCall = Expression.Call(null, genericMethodInfo, castMethodCall);
            var convertedResult = Expression.Convert(genericMethodCall, typeof(Task<object>));
            return convertedResult;
        }

        /// <summary>
        /// Cast Task of T to Task of object
        /// </summary>
        private static async Task<object> CastToObject<T>(Task<T> task)
        {
            return (object)await task;
        }

        private static Type GetTaskInnerTypeOrNull(Type type)
        {
            var genericType = ExtractGenericInterface(type, typeof(Task<>));

            return genericType?.GenericTypeArguments[0];
        }

        public static Type ExtractGenericInterface(Type queryType, Type interfaceType)
        {
            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (IsGenericInstantiation(queryType, interfaceType))
            {
                // queryType matches (i.e. is a closed generic type created from) the open generic type.
                return queryType;
            }

            // Otherwise check all interfaces the type implements for a match.
            // - If multiple different generic instantiations exists, we want the most derived one.
            // - If that doesn't break the tie, then we sort alphabetically so that it's deterministic.
            //
            // We do this by looking at interfaces on the type, and recursing to the base type
            // if we don't find any matches.
            return GetGenericInstantiation(queryType, interfaceType);
        }

        private static bool IsGenericInstantiation(Type candidate, Type interfaceType)
        {
            return
                candidate.GetTypeInfo().IsGenericType &&
                candidate.GetGenericTypeDefinition() == interfaceType;
        }

        private static Type GetGenericInstantiation(Type queryType, Type interfaceType)
        {
            Type bestMatch = null;
            var interfaces = queryType.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                if (IsGenericInstantiation(@interface, interfaceType))
                {
                    if (bestMatch == null)
                    {
                        bestMatch = @interface;
                    }
                    else if (StringComparer.Ordinal.Compare(@interface.FullName, bestMatch.FullName) < 0)
                    {
                        bestMatch = @interface;
                    }
                    else
                    {
                        // There are two matches at this level of the class hierarchy, but @interface is after
                        // bestMatch in the sort order.
                    }
                }
            }

            if (bestMatch != null)
            {
                return bestMatch;
            }

            // BaseType will be null for object and interfaces, which means we've reached 'bottom'.
            var baseType = queryType?.GetTypeInfo().BaseType;
            if (baseType == null)
            {
                return null;
            }
            else
            {
                return GetGenericInstantiation(baseType, interfaceType);
            }
        }

        private static Task<object> Convert<T>(object taskAsObject)
        {
            var task = (Task<T>)taskAsObject;
            return CastToObject<T>(task);
        }

        private static object[] GetParameterDefaultValues(ParameterInfo[] parameters)
        {
            var values = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo = parameters[i];
                object defaultValue;

                if (parameterInfo.HasDefaultValue)
                {
                    defaultValue = parameterInfo.DefaultValue;
                }
                else
                {
                    var defaultValueAttribute = parameterInfo
                        .GetCustomAttribute<DefaultValueAttribute>(inherit: false);

                    if (defaultValueAttribute?.Value == null)
                    {
                        defaultValue = parameterInfo.ParameterType.GetTypeInfo().IsValueType
                            ? Activator.CreateInstance(parameterInfo.ParameterType)
                            : null;
                    }
                    else
                    {
                        defaultValue = defaultValueAttribute.Value;
                    }
                }

                values[i] = defaultValue;
            }

            return values;
        }
    }
}