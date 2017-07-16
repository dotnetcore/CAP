using System;
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
        /// Publish a string message to specified topic.
        /// <para>
        ///  If you are using the EntityFramework, you need to configure the DbContextType first.
        ///  otherwise you need to use overloaded method with IDbConnection and IDbTransaction.
        /// </para>
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="content">message body content.</param>
        Task PublishAsync(string name, string content);

        /// <summary>
        /// Publis a object message to specified topic.        
        /// <para>
        ///  If you are using the EntityFramework, you need to configure the DbContextType first.
        ///  otherwise you need to use overloaded method with IDbConnection and IDbTransaction.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of conetent object.</typeparam>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">object instance that will be serialized of json.</param>
        Task PublishAsync<T>(string name, T contentObj);

        /// <summary>
        /// Publish a string message to specified topic with transacton.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="content">message body content.</param>
        /// <param name="dbConnection">the dbConnection of <see cref="IDbConnection"/></param>
        Task PublishAsync(string name, string content, IDbConnection dbConnection);

        /// <summary>
        /// Publish a string message to specified topic with transacton.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="content">message body content.</param>
        /// <param name="dbConnection">the connection of <see cref="IDbConnection"/></param>
        /// <param name="dbTransaction">the transaction of <see cref="IDbTransaction"/></param>
        Task PublishAsync(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction);
    }
}