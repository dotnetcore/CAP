using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    /// <summary>
    /// Defines an interface for model binders.
    /// </summary>
    public interface IModelBinder
    {
        Task<ModelBindingResult> BindModelAsync(string content);
    }
}