using System.Reflection;
using DotNetCore.CAP.Abstractions.ModelBinding;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// Model binder factory.
    /// </summary>
    public interface IModelBinderFactory
    {
        /// <summary>
        /// Create a model binder by parameter.
        /// </summary>
        /// <param name="parameter">The method parameter info</param>
        /// <returns>A model binder instance.</returns>
        IModelBinder CreateBinder(ParameterInfo parameter);
    }
}