using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Cap producer service for store message to database.
    /// </summary>
    public interface ICapProducerService
    {
        /// <summary>
        /// Send a message to cap job.
        /// </summary>
        /// <param name="topic">the topic name or exchange router key.</param>
        /// <param name="content">message body content.</param>
        Task SendAsync(string topic, string content);

        /// <summary>
        /// Send a message to cap job.
        /// </summary>
        /// <typeparam name="T">The type of conetent object.</typeparam>
        /// <param name="topic">the topic name or exchange router key.</param>
        /// <param name="contentObj">object instance that will be serialized of json.</param>
        /// <returns></returns>
        Task SendAsync<T>(string topic, T contentObj);
    }
}