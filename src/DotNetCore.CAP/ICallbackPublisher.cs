using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A callback that is sent to Productor after a successful consumer execution
    /// </summary>
    public interface ICallbackPublisher
    {
        /// <summary>
        /// Publish a callback message
        /// </summary>
        Task PublishAsync(CapPublishedMessage obj);
    }
}