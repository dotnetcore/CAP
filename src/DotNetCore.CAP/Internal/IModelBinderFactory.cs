using System.Reflection;
using DotNetCore.CAP.Abstractions.ModelBinding;

namespace DotNetCore.CAP.Internal
{
    public interface IModelBinderFactory
    {
        IModelBinder CreateBinder(ParameterInfo parameter);
    }
}
