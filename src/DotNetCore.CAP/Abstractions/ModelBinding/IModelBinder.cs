using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    public interface IModelBinder
    {
        Task BindModelAsync(ModelBindingContext bindingContext);
    }
}