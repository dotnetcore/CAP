using System.Reflection;
using DotNetCore.CAP.Abstractions.ModelBinding;

namespace DotNetCore.CAP.Abstractions
{
    public interface IModelBinderFactory
    {
        IModelBinder CreateBinder(ParameterInfo parameter);
    }
}