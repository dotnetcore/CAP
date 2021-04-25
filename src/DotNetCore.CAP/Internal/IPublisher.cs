
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DotNetCore.CAP.Internal
{

    public interface IPublisher<TR>
    {

        /// <summary>
        /// CAP transaction context object
        /// </summary>  
        AsyncLocal<ICapTransaction> Transaction { get; }
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Asynchronous publish an object message.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="callbackName">callback subscriber name</param>
        /// <param name="cancellationToken"></param>
        Task PublishAsync(string name, [CanBeNull] TR contentObj, string callbackName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronous publish an object message with custom headers
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="headers">message additional headers.</param>
        /// <param name="cancellationToken"></param>
        Task PublishAsync(string name, [CanBeNull] TR contentObj, IDictionary<string, string> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish an object message.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="callbackName">callback subscriber name</param>
        void Publish(string name, [CanBeNull] TR contentObj, string callbackName = null);

        /// <summary>
        /// Publish an object message.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="headers">message additional headers.</param>
        void Publish(string name, [CanBeNull] TR contentObj, IDictionary<string, string> headers);
    }
}
