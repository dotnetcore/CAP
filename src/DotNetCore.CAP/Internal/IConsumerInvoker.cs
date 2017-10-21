using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Perform user definition method of consumers.
    /// </summary>
    public interface IConsumerInvoker
    {
        /// <summary>
        /// Invoke consumer method whit consumer context.
        /// </summary>
        /// <param name="context">consumer execute context</param>
        Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context);
    }
}