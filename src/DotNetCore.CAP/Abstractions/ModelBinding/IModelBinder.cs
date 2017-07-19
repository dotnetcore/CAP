using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions.ModelBinding
{
    /// <summary>
    /// Defines an interface for model binders.
    /// </summary>
    public interface IModelBinder
    {
        /// <summary>
        /// Attempts to bind a model.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>
        /// <para>
        /// A <see cref="Task"/> which will complete when the model binding process completes.
        /// </para>
        /// </returns>
        Task BindModelAsync(ModelBindingContext bindingContext);
    }
}