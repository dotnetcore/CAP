using System.Data;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A publish service for publish a message to CAP.
    /// </summary>
    public interface ICapPublisher
    {
        /// <summary>
        /// (EntityFramework) Asynchronous publish a object message.
        /// <para>
        /// If you are using the EntityFramework, you need to configure the DbContextType first.
        /// otherwise you need to use overloaded method with IDbConnection and IDbTransaction.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of content object.</typeparam>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized of json.</param>
        /// <param name="callbackName">callback subscriber name</param>
        Task PublishAsync<T>(string name, T contentObj, string callbackName = null);

        /// <summary>
        /// (EntityFramework) Publish a object message.
        /// <para>
        /// If you are using the EntityFramework, you need to configure the DbContextType first.
        /// otherwise you need to use overloaded method with IDbConnection and IDbTransaction.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of content object.</typeparam>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized of json.</param>
        /// <param name="callbackName">callback subscriber name</param>
        void Publish<T>(string name, T contentObj, string callbackName = null);

        /// <summary>
        /// (ado.net) Asynchronous publish a object message.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized of json.</param>
        /// <param name="dbTransaction">the transaction of <see cref="IDbTransaction" /></param>
        /// <param name="callbackName">callback subscriber name</param>
        Task PublishAsync<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null);

        /// <summary>
        /// (ado.net) Publish a object message.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized of json.</param>
        /// <param name="dbTransaction">the transaction of <see cref="IDbTransaction" /></param>
        /// <param name="callbackName">callback subscriber name</param>
        void Publish<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null);
    }
}