using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A publish service for publish a message to CAP.
    /// </summary>
    public interface ICapPublisher
    {
        /// <summary>
        /// Publish a string message to specified topic.
        /// </summary>
        /// <param name="topic">the topic name or exchange router key.</param>
        /// <param name="content">message body content.</param>
        Task PublishAsync(string topic, string content);

        /// <summary>
        /// Publis a object message to specified topic.
        /// </summary>
        /// <typeparam name="T">The type of conetent object.</typeparam>
        /// <param name="topic">the topic name or exchange router key.</param>
        /// <param name="contentObj">object instance that will be serialized of json.</param>
        /// <returns></returns>
        Task PublishAsync<T>(string topic, T contentObj);
    }
}