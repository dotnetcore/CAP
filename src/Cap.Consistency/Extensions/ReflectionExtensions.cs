using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Cap.Consistency.Extensions
{
    public static class ReflectionExtensions
    {
        public static Type MakeDefType(this TypeInfo byRefTypeInfo)
        {
            if (byRefTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(byRefTypeInfo));
            }
            if (!byRefTypeInfo.IsByRef)
            {
                throw new ArgumentException($"Type {byRefTypeInfo} is not passed by reference.");
            }

            var assemblyQualifiedName = byRefTypeInfo.AssemblyQualifiedName;
            var index = assemblyQualifiedName.IndexOf('&');
            assemblyQualifiedName = assemblyQualifiedName.Remove(index, 1);

            return byRefTypeInfo.Assembly.GetType(assemblyQualifiedName, true);
        }
 
        public static bool CanInherited(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if (!typeInfo.IsClass || typeInfo.IsSealed)
            {
                return false;
            }

            if (typeInfo.IsNested)
            {
                return typeInfo.IsNestedPublic && typeInfo.DeclaringType.GetTypeInfo().IsPublic;
            }
            else
            {
                return typeInfo.IsPublic;
            }
        }

        internal static MethodInfo GetMethodBySign(this TypeInfo typeInfo, MethodInfo method)
        {
            if (method.IsGenericMethod)
            {
                foreach (var genericMethod in typeInfo.DeclaredMethods.Where(m => m.IsGenericMethod))
                {
                    if (method.ToString() == genericMethod.ToString())
                    {
                        return genericMethod;
                    }
                }
            }

            return typeInfo.GetMethod(method.Name, method.GetParameterTypes());
        }

        public static Type[] GetParameterTypes(this MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            return method.GetParameters().Select(parame => parame.ParameterType).ToArray();
        }

        public static bool IsPropertyBinding(this MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            return method.GetBindingProperty() != null;
        }

        public static PropertyInfo GetBindingProperty(this MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            foreach (var property in method.DeclaringType.GetTypeInfo().DeclaredProperties)
            {
                if (property.CanRead && property.GetMethod == method)
                {
                    return property;
                }

                if (property.CanWrite && property.SetMethod == method)
                {
                    return property;
                }
            }

            return null;
        }

        public static MethodInfo GetMethod<T>(Expression<T> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new InvalidCastException("Cannot be converted to MethodCallExpression");
            }
            return methodCallExpression.Method;
        }

        public static MethodInfo GetMethod<T>(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return typeof(T).GetTypeInfo().GetMethod(name);
        }

        internal static MethodInfo ReacquisitionIfDeclaringTypeIsGenericTypeDefinition(this MethodInfo methodInfo, Type closedGenericType)
        {
            if (!methodInfo.DeclaringType.GetTypeInfo().IsGenericTypeDefinition)
            {
                return methodInfo;
            }

            return closedGenericType.GetTypeInfo().GetMethod(methodInfo.Name, methodInfo.GetParameterTypes());
        }



        internal static string GetFullName(this MemberInfo member)
        {
            var declaringType = member.DeclaringType.GetTypeInfo();
            if (declaringType.IsInterface)
            {
                return $"{declaringType.Name}.{member.Name}".Replace('+', '.');
            }
            return member.Name;
        }

        internal static bool IsReturnTask(this MethodInfo methodInfo)
        {
            return typeof(Task).GetTypeInfo().IsAssignableFrom(methodInfo.ReturnType.GetTypeInfo());
        }

    }
}
