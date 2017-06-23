using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// Perform user definition method of consumers.
    /// </summary>
    public interface IConsumerInvoker
    {
        /// <summary>
        /// begin to invoke method.
        /// </summary>
        Task InvokeAsync();
    }
}